[<AutoOpen>]
module DnnTypes

type DnnSkin =
    | UnknownSkin
    | Gravity   // Platform default
    | Xcillion  // new Platform skin as of release 8.0.0

type InstallationLanguageas = 
    | English
    | German
    | Spanish
    | French
    | Italian
    | Dutch

type DnnInstallationMode = 
    | AutoNew
    | AutoUpgrade
    | WizardNew
    | WizardUpgrade

type DnnUser = 
    | Host
    | RegisteredUser of string * string // username * password

type RegisterUserInfo = 
    { UserName : string
      Password : string
      ConfirmPass : string
      DisplayName : string
      EmailAddress : string }

type CreateUserInfo = 
    { FirstName : string
      LastName : string
      UserName : string
      EmailAddress : string }

type PagePosition =  | BEFORE | AFTER | ATEND

type NewPageInfo = 
    { Name : string // make sure no spaces or specil characters here for bettr testability
      Title : string
      Description : string
      ParentPage : string // when nested names to be selected, use this format "parent/child1/subchild1"
      Position : PagePosition
      AfterPage : string // when top-level page, the page to insert after or before
      HeaderTags : string
      Theme : string
      Container : string
      RemoveFromMenu : bool
      GrantToAllUsers : bool
      GrantToRegisteredUsers : bool }

type PageReactTabs =
    | DetailsTab
    | PermissionsTab
    | AdvancedTab

type PageDetailsForm =
    {
        StandardType:string
        ExistingType:string
        URLType:string
        FileType:string
        Name: string 
        Title: string
        Description: string
        Keywords: string
        Tags: string
        TagsInput: string
        ParentPage: string
        DisplayInMenu:string
        LinkTracking: string
        EnableScheduling: string
        Workflow: string

        Existingpage: string
        PermanentRedirect:string
        openLinkInNewWindows: string

        ExternalUrl: string

        btBrowse:string
        btUpload:string
        btLink:string
        BrowseFileSystemFolder: string
        BrowseFileSystemFile: string

        URlLink: string
    }

type PagePermissionForm =
   {
        AllUsersView:string
        AllUsersAdd:string
        AllUsersAddConte:string
        AllUsersCopy:string
        AllUsersDelete:string
        AllUsersExport:string
        AllUsersImport:string

        AllUsersManageSettin:string
        AllUsersNaviga:string
        AllUsersEdit:string

        RegisteredUsersView:string
        RegisteredUsersAdd:string
        RegisteredUsersAddConte:string
        RegisteredUsersCopy:string
        RegisteredUsersDelete:string
        RegisteredUsersExport:string
        RegisteredUsersImport:string

        RegisteredUsersManageSettin:string
        RegisteredUsersNaviga:string
        RegisteredUsersEdit:string
   }

type TristateBool = 
    | NOCHANGE
    | FALSE
    | TRUE

type PageSettingDetails = 
    { Name : string
      Title : string
      RelativeUrl : string
      DoNotRedirect : TristateBool
      Description : string
      Keywords : string
      ParentPage : string
      IncludeInMenu : TristateBool }

type PermissionState = 
    | DONTCHANGE
    | CLEAR
    | GRANT
    | DENY // clear is equivalent to inherit

type PagePermissions = 
    { AllUsersViewPage : PermissionState
      AllUsersEditPage : PermissionState
      RegisteredUsersViewPage : PermissionState
      RegisteredUsersEditPage : PermissionState }

type UserRegistrationType =
    | NONE
    | PRIVATE
    | PUBLIC
    | VERIFIED

type FolderType =
    | STANDARD
    | SECURE
    | DATABASE

type ConnectorsList =
    | AZURE
    | UNC

type HostScheduler =
    { Name : string
      Enabled : bool
      Frequency : string
      RetryTime : string }

type ControlPanel =
    | CONTROLBAR
    | PERSONABAR
    | RIBBONBAR

type NameValuePair = { Name:string; Value:string }

type UserLoginInfo =
    { UserName : string
      Password : string
      DisplayName : string
      DNNCookie : NameValuePair
      RVCookie : NameValuePair
      RVToken : NameValuePair
    }

// We have 2 types for user creation & registration already above, with different content fields. 
// However, none of them are complete to fit for a common uses. 
// I created this API one and will add necessary fields here, hopefully it will be used to replace another 2 in the future. 
type APICreateUserInfo = 
    { 
      UserID : string
      FirstName : string
      LastName : string
      UserName : string
      Password : string
      EmailAddress : string 
      DisplayName : string
      Authorize : string
    }

type APICreateRoleInfo = 
    { 
      Id : string
      Name : string
      GroupId : string
      Description : string
      SecurityMode : string
      Status : string 
      IsPublic : string
      AutoAssign : string
      IsSystem : string
    }

type APIRoleName = 
    | HOSTUSER = 0
    | ADMINISTRATORS = 1
    | REGISTEREDUSERS = 4
    | SUBSCRIBERS = 7
    | TRANSLATORUS = 8
    | UNVERIFIED = 9
    | ANONYMOUS = -100

type CommunityTabs =
    | Answers
    | Blogs
    | Challenges
    | Discussions
    | Events
    | Ideas
    | Wiki

// This object captures the permission matrix for the item's visibility across products and user roles
type InfoArchItem =
    { 
      ItemSelector : string
      Host : bool  
      Admin : bool
    }

type ExportMode =
    | DIFFERENTIAL
    | FULL

type ExportItems = 
    { 
      Content : bool
      Assets : bool
      Users : bool
      Roles : bool
      Vocabularies : bool 
      PageTemplates : bool  
      ProfileProperties : bool
      Permissions : bool
      Extensions : bool
      IncludeDeletions : bool
      RunNow : bool
      Pages: bool
    }

type LicensingInfo =
    { WebServer : string
      LicenseType : string
      AccountEmail : string
      InvoiceNumber : string
      LicenseID : string
      LicenseKey : string
      ActivationKey : string
    }

type PublishChannel =
    | FACEBOOK
    | LINKEDIN
    | TWITTER
    | EMBED

type UploadOption =
    | FILEUPLOAD
    | BROWSE
    | URL

type UserInfo = 
    { 
      FirstName : string
      LastName : string
      UserName : string
      EmailAddress : string
      Password : string
      ConfirmPass: string
    }

type PromptCommand =
    | ADDMODULE | ADDROLES | CLEARCACHE | CLEARHISTORY | CLEARLOG
    | CLEARSCREEN | CLH | CLS | CONFIG | COPYMODULE
    | DELETEMODULE | DELETEPAGE | DELETEROLE | DELETEUSER | ECHO
    | EXIT | GETHOST | GETMODULE | GETPAGE | GETPORTAL
    | GETROLE | GETSITE | GETTASK | GETUSER | GOTO
    | HELP | LISTCOMMANDS | LISTMODULES | LISTPAGES | LISTPORTALS
    | LISTROLES | LISTSITES | LISTTASKS | LISTUSERS | MOVEMODULE
    | NEWPAGE | NEWROLE | NEWUSER | PURGEMODULE | PURGEPAGE
    | PURGEUSER | RELOAD | RESETPASSWORD | RESTARTAPPLICATION | RESTOREMODULE
    | RESTOREPAGE | RESTOREUSER | SETMODE | SETPAGE | SETROLE
    | SETTASK | SETUSER

type PagesContextMenu =
    | ADDPAGE
    | VIEW
    | EDIT
    | DUPLICATE
    | ANALYTICS

type DragType =
    | CHILD
    | FORMER
    | NEXT
