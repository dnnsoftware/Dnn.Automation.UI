module CreateChildSiteTest

open DnnCanopyContext
open DnnSiteCreate
open DnnAddToRole
open DnnManager

(*
 * NOTE: This file MUST contain a single test to create a child site and that is aall we need
 *       Afterwards, the main program will will run all regitered tests on this child site.
 *)

let private positive _ = 
    context "Create Child Site Tests"
    "Create childsite" @@@ fun _ -> 
        canopy.configuration.skipAllTestsOnFailure <- true
        createAutomationChildSite()
        clearStoredUsers()
        clearStoredFlags()
        clearBacklogSitePages()
        canopy.configuration.skipAllTestsOnFailure <- false
        useChildPortal <- true // from now on all tests are performed on child site

let all _ = 
    positive()
