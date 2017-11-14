module RecycleBin

open canopy
open DnnCanopyContext
open DnnManager
open DnnAddToRole
open DnnUserLogin
open DnnRecycleBin

let mutable private pageName = ""

let private adminTests _ =
    context "Recycle Bin : Admin Tests"

    "Recycle Bin | Admin | Delete a page from Pages section" @@@ fun _ ->        
        logOff()
        loginAsAdmin() |> ignore
        closePersonaBarIfOpen()
        pageName <- openNewPage false
        openPBPages()
        deletePage pageName

    "Recycle Bin | Admin | Restore a page from Recycle Bin" @@@ fun _ ->   
        restorePage pageName

    "Recycle Bin | Admin | Remove a page from Recycle Bin" @@@ fun _ ->
        reloadPage()
        openPBPages()
        deletePage pageName
        removePage pageName

    "Recycle Bin | Admin | Empty Recycle Bin" @@@ fun _ ->
        pageName <- openNewPage false
        openPBPages()
        deletePage pageName
        emptyRecycleBin()
        //verify page is deleted
        click "//li/a[@href='#pages']" //Pages tab of Recycle Bin
        waitForAjax()
        let pageDiv = sprintf "//div[.='%s']" pageName
        if existsAndVisible pageDiv then
            failwithf "  FAIL: Page %A is still visible after emptying Recycle Bin" pageName

let all _ =
    adminTests()      

