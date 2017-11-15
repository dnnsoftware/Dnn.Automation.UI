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

[<AutoOpen>]
module DnnSettings

open System

type ProductSettings() = class
    // defines all items that change from product/skin
    // to another, and populate with proper values.
    member val loginLinkId : string = null with get, set
    member val registerLinkId : string = null with get, set
    member val loggedinUserNameLinkId : string = null with get, set
    member val loggedinUserImageLinkId : string = null with get, set
    member val logoutLinkId : string = null with get, set
    member val logoutLinkIsDropDownMenu : bool = false with get, set
    member val specialRoles : string List = [] with get, set
    member val skin : DnnSkin = UnknownSkin with get, set
    member this.init(skinStr) =
        this.specialRoles <- ["Administrators"]
        this.skin <-
            match skinStr with
            | "Gravity" -> Gravity
            | "Xcillion" -> Xcillion
            | _ ->
                let tempSkin = DnnSkin.Gravity
                if not (String.IsNullOrEmpty skinStr) then 
                    printfn "  Set a default for unknown skin type [%s] as [%A]" skinStr tempSkin
                tempSkin

        match this.skin with
        | Xcillion
        | Gravity ->
            this.loginLinkId <- "#dnn_dnnLogin_enhancedLoginLink"
            this.logoutLinkId <- "#dnn_dnnLogin_enhancedLoginLink"
            this.registerLinkId <- "#dnn_dnnUser_enhancedRegisterLink"
            this.loggedinUserNameLinkId <- "#dnn_dnnUser_enhancedRegisterLink"
            this.loggedinUserImageLinkId <- "#dnn_dnnUser_avatar"
            this.logoutLinkIsDropDownMenu <- false
        | _ -> failwithf "Must be set to a known skin; unsupported skin [%A]" this.skin

        this
    end
