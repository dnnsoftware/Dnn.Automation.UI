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

module DnnCanopyContext

open System
open System.Diagnostics
open canopy
open DnnSiteLog

//=======================================================
// Set test fixtures setup/teardown functions
//=======================================================
let private stopWatch = Stopwatch()
let private wizardStr = if config.Site.UseInstallWizard then "wizard" else "auto"
let private upgradeStr = if config.Site.IsUpgrade then "upgrade" else "new"

let mutable isChildSiteContext = false

let (@@@) =
    if config.Settings.DiagMode then (&&&&) //highlights elements
    else (&&&)

let context name =
    let testFor = if isChildSiteContext then "CHILD" else "MAIN"
    canopy.runner.context (sprintf "{%s SITE} %s [%A %s-%s]" testFor name installationLanguage wizardStr upgradeStr)
    let mutable origWindow = ""
    once (fun _ ->
        origWindow <- browser.CurrentWindowHandle
        // reset context flag
        canopy.configuration.skipRemainingTestsInContextOnFailure <- false
        printfn "=== TestContext started @ %A ===" DateTime.Now)
    lastly (fun _ ->
        printfn "--- TestContext finished @ %A ---" DateTime.Now
        if origWindow <> browser.CurrentWindowHandle then
            // this might throw an exception and cause the tests to fail; keep an eye
            switchToOtherWindow origWindow
        )
    before (fun _ ->
        if canopy.configuration.skipAllTestsOnFailure then canopy.configuration.failureScreenshotsEnabled <- false
        stopWatch.Restart())
    after (fun _ ->
        stopWatch.Stop()
        let total = stopWatch.Elapsed
        printfn "Test excution time: %im %i.%03is" total.Minutes total.Seconds total.Milliseconds
        try
            analyzeLogFile()
        with ex ->
            // we can't throw exception here as this will hide the actual error of the test if any
            printfn "%s" ex.Message
        )
