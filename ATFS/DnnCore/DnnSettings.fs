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
