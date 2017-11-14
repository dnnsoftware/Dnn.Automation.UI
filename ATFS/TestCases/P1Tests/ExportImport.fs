module ExportImport

open canopy
open DnnCanopyContext
open System.Diagnostics
open DnnHost
open DnnScheduler
open DnnUserLogin

let mutable private exportPackName = ""

/// <summary>Opens the first job summary section</summary>
let private openFirstJob() = 
    let firstJob = "div.logContainerBox>div.collapsible-jobdetail:nth-of-type(2)>div"
    let isOpen = (element firstJob).GetAttribute("class").Contains("false")
    if not (isOpen) then 
        click (firstJob + ">div.term-header")
        waitForAjax()

/// <summary>
/// Checks if a job with a non-Completed status exists. 
/// If so, waits for its status to Complete for upto 1 minute.
/// </summary>
let private verifyImpExpJobCompleted()=
    let itemNotComplete = "//span[contains(@class,'job-status')]/div[@title!='Completed']"
    if exists itemNotComplete then        
        let theItem = element itemNotComplete
        let sw = Stopwatch.StartNew()
        let mutable retries = 20
        while theItem.GetAttribute("title") <> "Completed" && retries > 0 do
            sleep 1.5
            retries <- retries - 1
        sw.Stop()         
        let waitSecs = float(sw.ElapsedMilliseconds) / 1000.0
        if theItem.GetAttribute("title") <> "Completed" then
            failwithf "  FAIL: Import/Export Job did not complete in %f seconds" waitSecs
        else
            printfn "  INFO: Import/Export Job completed in %f seconds" waitSecs

/// <summary>
/// Reads and returns the Number items in the first job summary
/// </summary>
let private readJobSummaryItemNumbers() =
    openFirstJob()
    let leftColItem = "div.export-site-container>div.left-column>div.dnn-grid-cell:nth-of-type(numb)>div.import-summary-item"
    let rightColItem = "div.export-site-container>div.right-column>div.dnn-grid-cell:nth-of-type(numb)>div.import-summary-item"
    let users = (element (leftColItem.Replace("numb","1"))).Text
    let pages = (element (leftColItem.Replace("numb","2"))).Text
    let rolesGroups = (element (leftColItem.Replace("numb","3"))).Text
    let vocabs = (element (leftColItem.Replace("numb","4"))).Text
    let templates = (element (leftColItem.Replace("numb","5"))).Text
    let extensions = (element (rightColItem.Replace("numb","3"))).Text
    let assets = (element (rightColItem.Replace("numb","4"))).Text
    users, pages, rolesGroups, vocabs, templates, extensions, assets

let deleteCancelExportsite exportName =
    let statusOfTask = "//span[@class='job-status4']/div[@title='Cancelled']"
    waitForElementPresentXSecs statusOfTask 60.0
    click statusOfTask
    let exportname = sprintf"//div[@class='import-summary-item' and .='%s']" exportName
    waitForElementPresentXSecs exportname 60.0
    click "//div[@class='dnn-grid-cell action-buttons']/button[@role='secondary']"
    waitForElement "//div[@id='confirmation-dialog']"
    click "//div[@class='buttonpanel']/a[@id='confirmbtn']"

let private fullExportTests _ =
    context "Full Export Tests"  

    "Full Export | Verify Site can be Exported with All Contents from Site" @@@ fun _ ->
        loginAsHost()
        openPBImportExport()
        let exportItems = 
            { Content = true; Assets = true ; Users = true ; Roles  = true ; Vocabularies = true 
              PageTemplates = true  ; ProfileProperties = true ; Permissions = true ; 
              Extensions = true ; IncludeDeletions = true ; RunNow = false ; Pages = true }
        let siteName = if useChildPortal then childWebSiteName else parentWebSiteName
        exportPackName <- exportSite siteName FULL exportItems
        //verify job status is "Submitted" 
        if not(existsAndVisible "span.job-status0>div") then
            failwith "  FAIL: Export Job with Submitted Status was not found"

    "Full Export | Verify Export Job is Completed after Scheduler Run" @@@ fun _ ->
        openPBScheduler()
        canopy.configuration.skipRemainingTestsInContextOnFailure <- true
        runHostScheduler "Site Import/Export" |> ignore
        reloadPage()
        openPBImportExport()
        verifyImpExpJobCompleted()
        canopy.configuration.skipRemainingTestsInContextOnFailure <- false

    "Full Export | Verify Export Job Summary shows all content exported" @@@ fun _ ->
        reloadPage()
        openPBImportExport()
        let users, pages, rolesGroups, vocabs, templates, extensions, assets = readJobSummaryItemNumbers()
        let mutable failedReasons = ""
        let checkAllExported section (chkString:string)  = 
            let splitter = '/'
            //printfn "%s" chkString //debug
            let chkStrings = chkString.Trim().Replace(" ","").Split(splitter)
            if chkStrings.[0] <> chkStrings.[1] then
                failedReasons <- failedReasons + sprintf "\n\tNot all %s could be exported: %s" section chkString
        checkAllExported "Users" users; checkAllExported "Pages" pages; checkAllExported "Roles and Groups" rolesGroups;
        checkAllExported "Vocabularies" vocabs; checkAllExported "Page Templates" templates;
        checkAllExported "Extensions" extensions; checkAllExported "Assets" assets;
        if failedReasons <> "" then failwithf "  FAIL: %s" failedReasons
        logOff()

let private fullImportTests _ =
    context "Full Import Tests" 

    "Full Import | Verify User can Import an Exported Package" @@@ fun _ ->
        loginAsHost()
        reloadPage()
        canopy.configuration.skipRemainingTestsInContextOnFailure <- true
        openPBImportExport()        
        let siteName = if useChildPortal then childWebSiteName else parentWebSiteName
        importSite siteName exportPackName
        verifyImpExpJobCompleted()
        canopy.configuration.skipRemainingTestsInContextOnFailure <- false

    "Full Import | Verify Import Job Summary shows all content imported" @@@ fun _ ->
        reloadPage()
        openPBImportExport()
        let users, pages, rolesGroups, vocabs, templates, extensions, assets = readJobSummaryItemNumbers()
        let mutable failedReasons = ""
        let checkAllImported section (chkString:string)  = 
            let splitter = '/'
            let chkStrings = chkString.Trim().Replace(" ","").Split(splitter)
            failedReasons <- failedReasons + 
                if chkStrings.[0] <> chkStrings.[1] then sprintf "\n\tNot all %s could be imported: %s" section chkString
                else ""            
        checkAllImported "Users" users; checkAllImported "Pages" pages; checkAllImported "Roles and Groups" rolesGroups;
        checkAllImported "Vocabularies" vocabs; checkAllImported "Page Templates" templates;
        //checkAllImported "Extensions" extensions; //Importing to same site does not work for Extensions
        checkAllImported "Assets" assets;        
        if failedReasons <> "" then failwithf "  FAIL: %s" failedReasons
        logOff()

let private differentialExportTests _ =
    context "Differential Export Tests"  

    "Differential Export | Verify Site can be Exported with All Contents from Site" @@@ fun _ ->
        loginAsHost()
        openPBImportExport()
        let exportItems = 
            { Content = true; Assets = true ; Users = true ; Roles  = true ; Vocabularies = true 
              PageTemplates = true  ; ProfileProperties = true ; Permissions = true ; 
              Extensions = true ; IncludeDeletions = true ; RunNow = false ; Pages = true }
        let siteName = if useChildPortal then childWebSiteName else parentWebSiteName
        exportPackName <- exportSite siteName DIFFERENTIAL exportItems
        //verify job status is "Submitted" 
        if not(existsAndVisible "span.job-status0>div") then
            failwith "  FAIL: Export Job with Submitted Status was not found"

    "Differential Export | Verify Export Job is Completed after Scheduler Run" @@@ fun _ ->
        openPBScheduler()
        canopy.configuration.skipRemainingTestsInContextOnFailure <- true
        runHostScheduler "Site Import/Export" |> ignore
        reloadPage()
        openPBImportExport()
        verifyImpExpJobCompleted()
        canopy.configuration.skipRemainingTestsInContextOnFailure <- false

    "Differential Export | Verify Export Job Summary shows all content exported" @@@ fun _ ->
        reloadPage()
        openPBImportExport()
        let users, pages, rolesGroups, vocabs, templates, extensions, assets = readJobSummaryItemNumbers()
        let mutable failedReasons = ""
        let checkAllExported section (chkString:string)  = 
            let splitter = '/'
            //printfn "%s" chkString //debug
            let chkStrings = chkString.Trim().Replace(" ","").Split(splitter)
            if chkStrings.[0] <> chkStrings.[1] then
                failedReasons <- failedReasons + sprintf "\n\tNot all %s could be exported: %s" section chkString
        checkAllExported "Users" users; checkAllExported "Pages" pages; checkAllExported "Roles and Groups" rolesGroups;
        checkAllExported "Vocabularies" vocabs; checkAllExported "Page Templates" templates;
        checkAllExported "Extensions" extensions; checkAllExported "Assets" assets;
        if failedReasons <> "" then failwithf "  FAIL: %s" failedReasons
        logOff()

let private differentialImportTests _ =
    context "Differential Import Tests" 

    "Differential Import | Verify User can Import an Exported Package" @@@ fun _ ->
        loginAsHost()
        reloadPage()
        canopy.configuration.skipRemainingTestsInContextOnFailure <- true
        openPBImportExport()        
        let siteName = if useChildPortal then childWebSiteName else parentWebSiteName
        importSite siteName exportPackName
        verifyImpExpJobCompleted()
        canopy.configuration.skipRemainingTestsInContextOnFailure <- false

    "Differential Import | Verify Import Job Summary shows all content imported" @@@ fun _ ->
        reloadPage()
        openPBImportExport()
        let users, pages, rolesGroups, vocabs, templates, extensions, assets = readJobSummaryItemNumbers()
        let mutable failedReasons = ""
        let checkAllImported section (chkString:string)  = 
            let splitter = '/'
            let chkStrings = chkString.Trim().Replace(" ","").Split(splitter)
            failedReasons <- failedReasons + 
                if chkStrings.[0] <> chkStrings.[1] then sprintf "\n\tNot all %s could be imported: %s" section chkString
                else ""            
        checkAllImported "Users" users; checkAllImported "Pages" pages; checkAllImported "Roles and Groups" rolesGroups;
        checkAllImported "Vocabularies" vocabs; checkAllImported "Page Templates" templates;
        //checkAllImported "Extensions" extensions; //Importing to same site does not work for Extensions
        checkAllImported "Assets" assets;        
        if failedReasons <> "" then failwithf "  FAIL: %s" failedReasons
        logOff()

let all _ = 
    fullExportTests()
    fullImportTests()
    differentialExportTests()
    differentialImportTests()

