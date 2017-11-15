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
