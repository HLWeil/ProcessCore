module ProcessCore.Tests.Spreadsheet.Workbooks


open Fable.Pyxpecto
open ProcessCore
open CrossAsync
open ProcessCore.Helper
open ProcessCore.Tests.Fixtures
open TestingUtils
open ProcessCore.Table


let tests = testList "Workbooks" [

    let testBaseFolder =
        #if FABLE_COMPILER_JAVASCRIPT || FABLE_COMPILER_TYPESCRIPT
        "./tests/ProcessCore.Tests/TestObjects"
        #else
        Path.combine __SOURCE_DIRECTORY__ "../TestObjects"
        #endif

    testCaseCrossAsync "FacultativeCAM Investigation Read Write" (crossAsync {
        let p = Path.combine testBaseFolder "fct_investigation.xlsx"
        let! wb = Path.readFileXlsxAsync p 
        let ao = ScaffoldReader.Investigation.tryFromFsWorkbook (ARC) wb
        let a = Expect.wantSome ao "Assay should be read from workbook"
        let wb2 = ScaffoldReader.Investigation.toFsWorkbook a
        Expect.workBookEqual wb2 wb "Workbooks should be equal after read/write"  
    })
    testCaseCrossAsync "FacultativeCAM Study stri Read Write" (crossAsync {
        let p = Path.combine testBaseFolder "fct_stri_study.xlsx"
        let! wb = Path.readFileXlsxAsync p 
        let ao = ScaffoldReader.Study.tryFromFsWorkbook wb
        let a = Expect.wantSome ao "Study should be read from workbook"
        Expect.equal a.Tables.TableCount 1 "Study should have 1 table"
        let wb2 = ScaffoldReader.Study.toFsWorkbook a
        Expect.workBookEqual wb2 wb "Workbooks should be equal after read/write"  
    })
    testCaseCrossAsync "FacultativeCAM Assay GCqTOF Read Write" (crossAsync {
        let p = Path.combine testBaseFolder "fct_gcqtof_assay.xlsx"
        let! wb = Path.readFileXlsxAsync p 
        let ao = ScaffoldReader.Assay.tryFromFsWorkbook wb
        let a = Expect.wantSome ao "Assay should be read from workbook"
        Expect.equal a.Tables.TableCount 3 "Assay should have 3 tables"
        let wb2 = ScaffoldReader.Assay.toFsWorkbook a
        Expect.workBookEqual wb2 wb "Workbooks should be equal after read/write"  
    })
    testCaseCrossAsync "Ru_ChlamyHeatstress Datamap Proteomics Read Write" (crossAsync {
        let p = Path.combine testBaseFolder "ruch_proteomics_datamap.xlsx"
        let! wb = Path.readFileXlsxAsync p 
        let d = Spreadsheet.Datamap.fromFsWorkbook wb
        Expect.hasLength d.DataContexts 3 "Datamap should have 3 data contexts"
        let wb2 = Spreadsheet.Datamap.toFsWorkbook d
        Expect.workBookEqual wb2 wb "Workbooks should be equal after read/write"
    })

        
    ]
