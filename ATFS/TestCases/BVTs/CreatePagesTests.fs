module CreatePagesTests

open DnnCanopyContext
open DnnUserLogin
open DnnCreatePage

let mutable private pagUrl = "/"

let private preTest() = 
    goto "/"
    loginOnPageAs Host

let private postTest() = logOff()

//tests
let positive _ = 
    context "Basic main site page creation test"

    "Main site create new page (Top Level) test" @@@ fun _ -> 
        let postfixPageId = getRandomId()
        let pageInfo = 
            { Name = "TopPage" + postfixPageId
              Title = "TopPage" + postfixPageId
              Description = "Test Page " + postfixPageId + " description"
              ParentPage = ""
              AfterPage = homePageName
              Position = ATEND
              HeaderTags = "PageTag11,PageTag12"
              Theme = ""
              Container = ""
              RemoveFromMenu = true
              GrantToAllUsers = true
              GrantToRegisteredUsers = true }
        preTest()
        createPage pageInfo |> ignore
        closeEditMode()
        ensurePageWasCreated pageInfo.Name

let all _ = positive()
