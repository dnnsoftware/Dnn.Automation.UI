module DnnCreatePage

open DnnManager

// must be logged in with a suitable user
// "parentPage" parameter should be passed as appears in the pages tree
// when nested names to be selected, use this format "parent/child1/subchild1"
let createPage (pageInfo : NewPageInfo) = 
    let newPageUrl = createPagePB pageInfo null    
    printfn "  Page %A created successfully!" newPageUrl
    newPageUrl

// assertion to make sure we have no error in page creation dialog
let ensurePageWasCreated pageName =
    if existsAndVisible SkinMsgErrorSelector then failwithf "Error creating new page: %s" pageName

// assertion to make sure we do have an error in page creation dialog
let EnsurePageWasNotCreated pageName =
    if not (existsAndVisible SkinMsgErrorSelector) then
        failwithf "Must not be able to create already existing page: %s" pageName

let private getRandomId() = getRandomId()

/// <summary>Get a NewPageInfo object based on page prefix</summary>
/// <param name="pageprefix">The name and title prefix to be set for a new page in the NewPageInfo object</param>
let getNewPageInfo pageprefix =
    let random = getRandomId()
    let pageInfo : NewPageInfo = 
        { Name = pageprefix + random
          Title = pageprefix + random
          Description = pageprefix + random
          ParentPage = ""
          Position = PagePosition.ATEND
          AfterPage = ""
          HeaderTags = ""
          Theme = ""
          Container = ""
          RemoveFromMenu = true
          GrantToAllUsers = true
          GrantToRegisteredUsers = true }
    pageInfo
