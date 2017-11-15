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

module PageSettingsTests

open DnnCanopyContext
open DnnUserLogin
open DnnPageSettings

let private testPageName = homePageName
let private testPageUrl = "/"

//tests
let positive _ = 
    //============================================================
    context "Test Page Settings"

    "Page Settings | Modify Page Details" @@@ fun _ -> 
        loginAsHost()
        let settings = 
            { Name = testPageName
              Title = testPageName + " Title"
              RelativeUrl = testPageUrl
              DoNotRedirect = NOCHANGE
              Description = ""
              Keywords = "New Keyword 1,New Keyword2"
              ParentPage = ""
              IncludeInMenu = TRUE }
        modifyPageDetails testPageUrl settings
        //saveSettings()

    //Test case disabled since modifyPermissions not implemented yet
    //"Page Settings | Modify Permissions" @@@ fun _ -> 
    //    loginAsHost()
    //    let settings = 
    //        { AllUsersViewPage = GRANT
    //          AllUsersEditPage = CLEAR
    //          RegisteredUsersViewPage = DONTCHANGE
    //          RegisteredUsersEditPage = DONTCHANGE }
    //    modifyPermissions testPageUrl settings
    //    //saveSettings()

(*
    "Page Settings | Modify Advanced Settings" @@@ fun _ ->
        openPageSettings testPageUrl
        modifyAdvancedSettings testPageUrl settings
        saveSettings()

    "Page Settings | After All Tests" @@@ fun _ ->
        logOff()
    *)

let all _ = positive()
