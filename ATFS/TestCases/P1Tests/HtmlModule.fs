﻿// Automation UI Testing Framework - ATFS - http://www.dnnsoftware.com
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

module HtmlModule

open DnnCanopyContext
open DnnManager
open DnnHtmlModule
open DnnUserLogin
open InputSimulatorHelper

let htmlModuleTests _ =
    context "HTML Module Tests"

    "HTML Module | Host | Publish text in HTML Module" @@@ fun _ ->
        logOff()        
        loginAsHost()
        openNewPage true |> ignore //new page has HTML module already
        insertTextHTML null "Publish text in HTML Module" (loremIpsumText.Substring(0, 500)) false

let htmlCommonTests _ =
    context "HTML Common Tests"

    "HTML and HTML Pro Modules | Host | Token replacement test" @@@ fun _ ->
        logOff()
        loginAsHost()
        openNewPage true |> ignore 
        openEditMode()               
        //insert tokens
        insertTextHTML null "Token Replacement test" "[User:UserName] [User:FirstName] [User:LastName]" true
        enableTokenReplacement true 0 
        closeEditMode()
        hardReloadPage()
        let expectedText = sprintf "%s %s" config.Site.HostUserName config.Site.HostDisplayName
        verifyHtmlModuleText expectedText        

let all _ = 
    htmlModuleTests()
    htmlCommonTests()
