// Automation UI Testing Framework - ATFS - http://www.dnnsoftware.com
// Copyright (c) 2015 - 2017, DNN Corporation
// All rights reserved.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and 
// to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions 
// of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
// TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.

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
