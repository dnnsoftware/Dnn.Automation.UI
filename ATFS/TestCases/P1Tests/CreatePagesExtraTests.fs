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
