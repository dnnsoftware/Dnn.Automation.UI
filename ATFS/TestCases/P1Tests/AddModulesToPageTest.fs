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
