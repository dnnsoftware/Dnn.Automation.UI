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
    //if config.Settings.DevMode then (&&&&) //highlights elements
    //else (&&&)
    (&&&)

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
