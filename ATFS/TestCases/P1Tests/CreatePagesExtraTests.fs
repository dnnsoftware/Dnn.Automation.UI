module CreatePagesExtraTests

open DnnCanopyContext
open DnnUserLogin
open DnnCreatePage
open DnnManager

let mutable private level1PageName = ""
let mutable private level2PageName = ""
let mutable private pagUrl = "/"
let mutable private failSubsequentTests = true

let private preTest() = 
    pagUrl <- ""
    goto "/"
    loginOnPageAs Host

let private postTest() = logOff()

let mutable private postfixPageId = ""

//tests
let positive _ = 
    //============================================================
    context "Test main site page creation - 3 levels"
    "Main site create new page (Level 1) test" @@@ fun _ -> 
        let createLevel1Page() = 
            postfixPageId <- getRandomId()
            let pageInfo = 
                { Name = "TopPage" + postfixPageId
                  Title = ""
                  Description = "Test Page " + postfixPageId + " description"
                  ParentPage = ""
                  Position = ATEND
                  AfterPage = homePageName
                  HeaderTags = "PageTag11,PageTag12"
                  Theme = ""
                  Container = ""
                  RemoveFromMenu = true
                  GrantToAllUsers = true
                  GrantToRegisteredUsers = true }
            goto pagUrl
            createPage pageInfo |> ignore
            ensurePageWasCreated pageInfo.Name
            level1PageName <- pageInfo.Name
            pagUrl <- ("/" + pageInfo.Name)

        canopy.configuration.skipRemainingTestsInContextOnFailure <- true
        preTest()
        createLevel1Page()
        canopy.configuration.skipRemainingTestsInContextOnFailure <- false

    "Main site create new page (Level 2) test" @@@ fun _ -> 
        let createLevel2Page() = 
            let pageInfo = 
                { Name = "ChildPage" + postfixPageId
                  Title = ""
                  Description = ""
                  ParentPage = level1PageName
                  Position = ATEND
                  AfterPage = ""
                  HeaderTags = "PageTag21"
                  Theme = ""
                  Container = ""
                  RemoveFromMenu = true
                  GrantToAllUsers = false
                  GrantToRegisteredUsers = false }
            goto pagUrl
            createPage pageInfo |> ignore
            ensurePageWasCreated pageInfo.Name
            level2PageName <- pageInfo.Name
            pagUrl <- ("/" + pageInfo.ParentPage + "/" + pageInfo.Name)

        canopy.configuration.skipRemainingTestsInContextOnFailure <- true
        createLevel2Page()
        canopy.configuration.skipRemainingTestsInContextOnFailure <- false

    "Main site create new page (Level 3) test" @@@ fun _ -> 
        let createLevel3Page() = 
            let pageInfo = 
                { Name = "SubChildPage" + postfixPageId
                  Title = ""
                  Description = ""
                  ParentPage = level2PageName
                  Position = ATEND
                  AfterPage = ""
                  HeaderTags = ""
                  Theme = ""
                  Container = ""
                  RemoveFromMenu = true
                  GrantToAllUsers = false
                  GrantToRegisteredUsers = false }
            goto pagUrl

            let newPageUrl = createPagePB pageInfo level1PageName 

            //createPage pageInfo
            ensurePageWasCreated pageInfo.Name
            pagUrl <- ("/" + pageInfo.ParentPage + "/" + pageInfo.Name)

        createLevel3Page()
        goto pagUrl
        postTest()

let negative _ = 
    //============================================================
    context "Test Page creation failure"
    "Create already existing page should fail" @@@ fun _ ->
        let createExistingage() = 
            let pageInfo = 
                { Name = homePageName
                  Title = ""
                  Description = ""
                  ParentPage = ""
                  Position = ATEND
                  AfterPage = homePageName
                  HeaderTags = ""
                  Theme = ""
                  Container = ""
                  RemoveFromMenu = false
                  GrantToAllUsers = false
                  GrantToRegisteredUsers = false }
            level1PageName <- pageInfo.Name
            try
                createPage pageInfo |> ignore
                true
            with _ ->
                false
            //EnsurePageWasNotCreated level1PageName

        preTest()
        if createExistingage() then
            failwithf "Must not be able to create already existing page: %s" homePageName
        postTest()

let all _ =
    positive()
    negative()
