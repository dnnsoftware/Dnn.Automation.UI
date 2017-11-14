module InspectPagesTests

open canopy
open DnnCanopyContext
open DnnUserLogin
open DnnVisitPages

/// <summary>Testcase helper to test if a filepath exists.</summary>
/// <param name="filepath">The path of the file to be checked for existance.</param>
let private testFilesDeleted filepath =
    try
        goto filepath
    with _ -> ()
    if not(existsAndVisible browserServerAppError) then
        let error = sprintf "File %A was not deleted post install or upgrade. " filepath
        raise (System.Exception(error))

//tests
let private navbartests _ = 
    //============================================================
    context "Test Admin & Host pages"
    "Visit all NavBar pages on main portal" @@@ fun _ -> 
        loginOnPageAs Host

        goto "/Admin"
        let adminDiv = element "//div[contains(@id,'_ViewConsole_Console') and (@class='console normal')]"
        let adminPages = collectLinks ("#" + adminDiv.GetAttribute("id"))
        printfn "  Found %d site links under ADMIN page" adminPages.Length

        goto "/Host"
        let hostDiv = element "//div[contains(@id,'_ViewConsole_Console') and (@class='console normal')]"
        let hostPages = collectLinks ("#" + hostDiv.GetAttribute("id"))
        printfn "  Found %d site links under HOST page" adminPages.Length

        let failedPages = visitPages (adminPages @ hostPages)
        if failedPages.Length > 0 then
            goto failedPages.Head  // so we capture its image
            failwith "Admin and Host pages visits failed!"

let private deletedInstallFilesTest _ =
    context "Test Install & Upgrade pages deleted"
    "Verify install.aspx, installwizard.aspx, and upgradewizard.aspx are deleted" @@@ fun _ -> 
        logOff()
        let filesToCheck = ["/Install/Install.aspx"; "/Install/InstallWizard.aspx"; "/Install/UpgradeWizard.aspx";]
        let mutable failed = false
        let mutable failReasons = ""
        filesToCheck |> List.iter (fun item ->
            try
                testFilesDeleted item
            with ex ->
                failed <- true
                failReasons <- failReasons + ex.Message
        )
        if failed then failwithf "  FAIL: %s" failReasons

let all _ = 
    deletedInstallFilesTest()
