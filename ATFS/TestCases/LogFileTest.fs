module LogFileTest

open System
open DnnCanopyContext
open DnnSiteLog

let positive _ = 
    context "Log File Check"
    // MUST be the last test to run in all suites
    "Check log file contents" @@@ fun _ -> 
        let errors = getLogFileErrors()
        if errors.Length > 0 then 
            canopy.configuration.failureScreenshotsEnabled <- false
            let errmsg = sprintf "  Found %d error(s) in the site's log file:\n\t%s" errors.Length (String.Join("\n\t", errors))
            failwith errmsg

let all _ = positive()
