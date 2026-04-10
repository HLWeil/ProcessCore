-- Refresh the Paths / PathSteps closure tables.
-- Run this after seeding, or after any change to Process,
-- Material.name, or Data.path/selector.
--
-- Algorithm:
--   1. Walk the process graph from every root node via a recursive CTE.
--   2. For each walk state, carry three things in parallel:
--        - path_id   : concat of node ids, identifies the walk deterministically
--        - steps_json: JSON array of step objects built incrementally
--        - path_rendered: display string (node names joined with ' -> ')
--   3. Filter to leaf walks (current node has no outgoing Process).
--   4. Write one row per leaf walk into `Paths`.
--   5. Unroll each leaf walk's steps_json into one row per step in `PathSteps`.
--
-- The walk is computed once into a TEMP table, then consumed by both INSERTs.

BEGIN;

DROP TABLE IF EXISTS temp._leaf_walks;

CREATE TEMP TABLE _leaf_walks AS
WITH RECURSIVE walk(
    path_id, path_rendered, steps_json, current_type, current_id, depth
) AS (
    -- Base: first edge from each root node (inputs that are never outputs)
    SELECT
        p.input_type || ':' || p.input_id || '|' || p.output_type || ':' || p.output_id,
        in_nr.node_name || ' -> ' || out_nr.node_name,
        json_array(json_object(
            'step',        0,
            'process_id',  p.id,
            'input_type',  p.input_type,  'input_id',  p.input_id,
            'output_type', p.output_type, 'output_id', p.output_id
        )),
        p.output_type, p.output_id,
        0
    FROM Process p
    JOIN NodeRef in_nr
      ON in_nr.node_type = p.input_type
     AND in_nr.node_id   = p.input_id
    JOIN NodeRef out_nr
      ON out_nr.node_type = p.output_type
     AND out_nr.node_id   = p.output_id
    WHERE NOT EXISTS (
        SELECT 1 FROM Process prev
        WHERE prev.output_type = p.input_type
          AND prev.output_id   = p.input_id
    )

    UNION ALL

    -- Recursive: append one more edge
    SELECT
        w.path_id || '|' || p.output_type || ':' || p.output_id,
        w.path_rendered || ' -> ' || out_nr.node_name,
        json_insert(w.steps_json, '$[#]', json_object(
            'step',        w.depth + 1,
            'process_id',  p.id,
            'input_type',  p.input_type,  'input_id',  p.input_id,
            'output_type', p.output_type, 'output_id', p.output_id
        )),
        p.output_type, p.output_id,
        w.depth + 1
    FROM walk w
    JOIN Process p
      ON p.input_type = w.current_type
     AND p.input_id   = w.current_id
    JOIN NodeRef out_nr
      ON out_nr.node_type = p.output_type
     AND out_nr.node_id   = p.output_id
    WHERE w.depth < 100
)
SELECT
    w.path_id,
    w.path_rendered,
    w.steps_json,
    w.current_type AS leaf_type,
    w.current_id   AS leaf_id,
    w.depth + 1    AS length
FROM walk w
WHERE NOT EXISTS (
    SELECT 1 FROM Process p
    WHERE p.input_type = w.current_type
      AND p.input_id   = w.current_id
);

DELETE FROM PathSteps;
DELETE FROM Paths;

INSERT INTO Paths (path_id, length, root_type, root_id, leaf_type, leaf_id, path_rendered)
SELECT
    path_id,
    length,
    json_extract(steps_json, '$[0].input_type'),
    json_extract(steps_json, '$[0].input_id'),
    leaf_type,
    leaf_id,
    path_rendered
FROM _leaf_walks;

INSERT INTO PathSteps (path_id, step, process_id, input_type, input_id, output_type, output_id)
SELECT
    lw.path_id,
    CAST(json_extract(s.value, '$.step')        AS INTEGER),
    json_extract(s.value, '$.process_id'),
    json_extract(s.value, '$.input_type'),
    json_extract(s.value, '$.input_id'),
    json_extract(s.value, '$.output_type'),
    json_extract(s.value, '$.output_id')
FROM _leaf_walks lw, json_each(lw.steps_json) s;

DROP TABLE _leaf_walks;

COMMIT;
