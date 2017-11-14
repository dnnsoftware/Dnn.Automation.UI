module SupportIssues

open canopy
open DnnCanopyContext
open DnnAddToRole
open DnnCreatePage
open DnnAddModuleToPage
open DnnHost

let mutable testPassed = false
let mutable modulesOnPage = 0
let mutable hostModuleOnPage = false
let mutable deployedModuleId = 0
let mutable deployedPageUrl = ""
let mutable pageUrl = ""
let mutable modID =""
let mutable resourcefolder =""

let verifyModuleForUser hostmodule modulename displayname deployedPageUrl=
        let modId, pgUrl = deployModuleOnPage hostmodule modulename displayname deployedPageUrl 
        deployedModuleId <- modId

let SupportIssue _ =
    context "CONTENT-6159 SI: Module hidden to Admin Users (Amrit)"

    "Preconditions for Module hidden to Admin Users " @@@ fun _ ->
        loginAsHost() |> ignore
        let newPage = getNewPageInfo "TestPage"      
        createPage newPage |> ignore
        pageUrl <- currentUrl()
        verifyModuleForUser false "DNNCorpGoogleAnalytics" "Google Analytics Professional" pageUrl
        changeModulePermission pageUrl 1 3 8 2

    "Verify the Module is visible to Admin " @@@ fun _ ->    
        loginAsAdmin() |> ignore
        goto pageUrl
        let moduleonPage = sprintf"//a[@name='%i']/.." deployedModuleId
        if not (existsAndVisible moduleonPage) then
            failwith "  FAIL: Module is not visible to Admin"

let all _ =
    SupportIssue()
