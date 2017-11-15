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

module DnnSecurityAnalyzer

open System
open canopy

/// <summary> Check Audit exists and the run result. </summary>
/// <param name="auditCheckName"> The name of the Audit Check. </param>
/// <param name="auditResult"> The expected result of the Audit Check. 0 for fail, 1 for pass, 2 for warning. </param>
/// <returns> The reasons for failure, if any. </returns>
let private checksAuditResult (auditCheckName, auditResult) =
    let mutable failedReasons = ""
    let auditSelector = sprintf "//span[contains(text(),'%s')]" auditCheckName
    if exists auditSelector && not (existsAndVisible auditSelector) then scrollTo auditSelector
    if not (existsAndVisible auditSelector) then failedReasons <- failedReasons + sprintf "\tAudit Check %A not displayed.\n" auditCheckName
    //Audit Check "CheckSuperuserOldPassword" shows "alert" status because Site Images are more than 6 months old.
    if not (auditCheckName.Equals("CheckSuperuserOldPassword", StringComparison.InvariantCultureIgnoreCase)) then
        let mutable auditExpResult = 
            match auditResult with
            | 0 -> "fail"
            | 1 -> "pass"
            | 2 -> "alert"
            | 3 -> "[ Check ]"
            | _ -> failwithf "Unknown auditResult value of %d" auditResult
        let auditResultDiv = auditSelector + (sprintf "/../../../div[2]/div/span/div/div[@class='label-result-severity-%s']" auditExpResult)
        if not (exists auditResultDiv) then 
            let mutable actualResult = "something else"
            let actualResultDiv = auditSelector + "/../../../div[2]/div/span/div/div[@class='label-result-severity-actualresult']"
            if exists (actualResultDiv.Replace("actualresult", "fail")) then actualResult <- "fail"
            elif exists (actualResultDiv.Replace("actualresult", "pass")) then actualResult <- "pass"
            elif exists (actualResultDiv.Replace("actualresult", "alert")) then actualResult <- "alert"
            //fail test only if result is worse than expected, i.e. expecting alert, but actual was fail. If the actual was pass, then don't fail.
            if (auditExpResult = "pass" || (auditExpResult = "alert" && actualResult = "fail")) then 
                failedReasons <- failedReasons + sprintf "\tExpected result for audit check %A was %s, but actual result was %s.\n" auditCheckName auditExpResult actualResult
    failedReasons

/// <summary>Checks the number of scanner result items in a table</summary>
/// <param name="table">Selector of the table to check</param>
/// <returns>Number of actual items found</returns>
let private getScannerItemsCount table =
    if existsAndVisible table then
        scrollTo table
        ((element table) |> elementsWithin "div.scannerCheckItem").Length
    else 0

/// <summary> Test case helper to test Security Analyzer module. </summary>
/// <param name="tabNumber"> The tab number of the Security Analyzer module. Int value from 1 to 5. </param>
/// <returns> The reasons for failure, if any. </returns>
let testSecurityAnalyzer tabNumber =    
    scrollToOrigin()
    let securityAnalyzerTab = "div.securitySettings-app>div>div>div>div>ul>li:nth-child(3)"
    waitClick securityAnalyzerTab
    waitForAjax()
    let mutable failedReasons = ""
    match tabNumber with
    | 1 -> 
        click "div.dnn-tabs.secondary>ul>li:nth-child(1)"
        waitForAjax()
        //check security audit checks against expected results
        failedReasons <- listSecurityAuditCheck |> List.fold (fun acc item -> acc + (checksAuditResult item)) failedReasons
    | 2 | _ -> 
        let fileSystemTable = "//div[@class='scannercheck-topbar']/../div[2]/div[@class='scannerCheckItems']"
        let searchWord = "asp.net"
        let doScan()=
            try
                click "div.dnn-tabs.secondary>ul>li:nth-child(2)"
                waitForAjax()
                //search filename                
                "div.dnn-search-box>input" << searchWord
                sleep 0.5
                waitForAjax()
                waitLoadingBar()                
                waitForElementPresentXSecs fileSystemTable 40.0
                true
            with _ ->
                reloadPage()
                openPBSecurity()
                waitClick securityAnalyzerTab
                waitForAjax()
                false
        let scanSuccess = retryWithWait 3 0.5 doScan
        if not scanSuccess then
            failwithf "  FAIL: Search did not complete in Scanner Check."      
        //Files
        let numFiles = getScannerItemsCount fileSystemTable
        if numFiles < 40 then failedReasons <- failedReasons + sprintf "\tNumber of files matching keyword %A was %i, expected at least 40.\n" searchWord numFiles
        //DB Entries
        let numDBEntries = getScannerItemsCount "//div[@class='scannercheck-topbar']/../div[3]/div[@class='scannerCheckItems']"
        if numDBEntries < 1 then failedReasons <- failedReasons + sprintf "\tNumber of DB entries matching keyword %A was %i, expected at least 1.\n" searchWord numDBEntries 
        ///Search recently modified files
        scrollToOrigin()
        click "div.scannercheck-topbar>div.files-filter"
        waitForAjax()
        //High Risk Files
        let numHRFiles = getScannerItemsCount "//div[@class='scannercheck-topbar']/../div[2]/div[@class='scannerCheckItems-riskFiles']"
        if numHRFiles < 5 then failedReasons <- failedReasons + sprintf "\tNumber of High Risk Files was %i, expected at least 5.\n" numHRFiles
        //Low Risk Files
        let numLRFiles = getScannerItemsCount "//div[@class='scannercheck-topbar']/../div[3]/div[@class='scannerCheckItems-riskFiles']"
        if numLRFiles < 10 then failedReasons <- failedReasons + sprintf "\tNumber of Low Risk Files was %i, expected at least 10.\n" numLRFiles
        ///Search recently modified settings
        scrollToOrigin()
        click "div.scannercheck-topbar>div.settings-filter"
        waitForAjax()
        //Portal Settings
        let numPortalSettings = getScannerItemsCount "//div[@class='scannercheck-topbar']/../div[2]/div[@class='scannerCheckItems-portalSettings']"
        if numPortalSettings < 5 then failedReasons <- failedReasons + sprintf "\tNumber of Portal Settings was %i, expected at least 5.\n" numPortalSettings            
        //Host Settings
        let numHostSettings = getScannerItemsCount "//div[@class='scannercheck-topbar']/../div[3]/div[@class='scannerCheckItems-hostSettings']"
        if numHostSettings < 5 then failedReasons <- failedReasons + sprintf "\tNumber of Host Settings was %i, expected at least 5.\n" numHostSettings
        //Tab Settings
        let numTabSettings = getScannerItemsCount "//div[@class='scannercheck-topbar']/../div[4]/div[@class='scannerCheckItems-tabSettings']"
        let numExpMinTabSettings = 1
        if numTabSettings < numExpMinTabSettings then failedReasons <- failedReasons + sprintf "\tNumber of Tab Settings was %i, expected at least %i.\n" numTabSettings numExpMinTabSettings
        //Module Settings
        let numModuleSettings = getScannerItemsCount "//div[@class='scannercheck-topbar']/../div[5]/div[@class='scannerCheckItems-moduleSettings']"
        if numModuleSettings < 5 then failedReasons <- failedReasons + sprintf "\tNumber of Module Settings was %i, expected at least 5.\n" numModuleSettings
        //search SuperUser Activity
        scrollToOrigin()
        click "div.dnn-tabs.secondary>ul>li:nth-child(3)"
        waitForAjax()
        let uNameSpanText = (element "div.label-username>div>span").Text
        if uNameSpanText.Contains("host") |> not then
            failedReasons <- failedReasons + "\tUsername \"host\" was not found in the list of SuperUser Activity\n"
    failedReasons
