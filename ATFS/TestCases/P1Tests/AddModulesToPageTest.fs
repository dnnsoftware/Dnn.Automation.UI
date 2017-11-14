module AddModulesToPageTest

open DnnCanopyContext
open DnnUserLogin
open DnnCreatePage
open DnnAddModuleToPage
open DnnHtmlModule

let mutable private level1PageName = ""
let mutable private pagUrl = "/"
let mutable private failSubsequentTests = true

let private preTest() = 
    goto "/"
    loginOnPageAs Host

let private postTest() = logOff()

//tests
let positive _ = 
    context "Test adding HTML module to page"

    "Create new page (Level 1) test" @@@ fun _ -> 
        let createLevel1Page() = 
            let postfix = getRandomId()
            let pageInfo = 
                { Name = "Page" + postfix
                  Title = ""
                  Description = "Test Page " + postfix + " description"
                  ParentPage = ""
                  Position = ATEND
                  AfterPage = homePageName
                  HeaderTags = "PageTag11,PageTag12"
                  Theme = ""
                  Container = ""
                  RemoveFromMenu = true
                  GrantToAllUsers = true
                  GrantToRegisteredUsers = true }
            preTest()
            level1PageName <- pageInfo.Name
            createPage pageInfo |> ignore
            ensurePageWasCreated level1PageName
            pagUrl <- ("/" + level1PageName)

        canopy.configuration.skipRemainingTestsInContextOnFailure <- true
        createLevel1Page()
        canopy.configuration.skipRemainingTestsInContextOnFailure <- false

    "Add HTML Module to page test" @@@ fun _ -> 
        let addHtmlModuleToPlatformPage() = 
            let moduleId = addModuleToPlatformPage pagUrl "DNN_HTML" "HTML"

            let textToInsert = sprintf "Sample Content - HTML Module Id %d" moduleId
            insertTextHTML (moduleId.ToString()) "Add Module Test" textToInsert false

            if not (isAddModuleSuceccessful()) then failwithf "Error adding HTML module to page %s" level1PageName

            let textToInsert = sprintf "Sample Content - HTML Module Id %d" moduleId
            insertTextHTML (moduleId.ToString()) "Add Module Test" textToInsert false

            if not (isAddModuleSuceccessful()) then failwithf "Error adding HTML module to page %s" level1PageName

        canopy.configuration.skipRemainingTestsInContextOnFailure <- true

        addHtmlModuleToPlatformPage()

        canopy.configuration.skipRemainingTestsInContextOnFailure <- false
        postTest()

let all _ = positive()
