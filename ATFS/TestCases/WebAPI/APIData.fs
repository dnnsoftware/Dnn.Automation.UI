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

module APIData

open DnnTypes

let [<Literal>] SamplePostStyleSheet = """ {"portalId":"portalIdReplaceMe","styleSheetContent":"/* \n * Modified Deprecated DNN CSS class names will remain available for some time\n * before being  permanently removed. Removal will occur according to\n * the  following process:\n *\n * 1. Removal will only occur with a major (x.y) release, never\n *    with a maintenance (x.y.z) release.\n * 2. Removal will not occur less than six months after the release\n *    when it was deprecated.\n * 3. Removal will not occur until after deprecation has been noted \n *    in at least two major releases.\n *\n *                                              |        |Planned |\n *  Name                                        |Release |Removal |\n *---------------------------------------------- -------- -------- \n * Mod{NAME}C                                     5.6.2    6.2\n *   {NAME} = sanitized version of the DesktopModule Name \n *   Used on <div> tag surrounding Module Content, inside container\n *---------------------------------------------- -------- -------- \n */  \n\n\n\n\n/* PAGE BACKGROUND */\n/* background color for the header at the top of the page  */\n.HeadBg {\n}\n\n/* background color for the content part of the pages */\nBody\n{\n}\n\n.ControlPanel {\n}\n\n/* background/border colors for the selected tab */\n.TabBg {\n}\n\n.LeftPane  { \n}\n\n.ContentPane  { \n}\n\n.RightPane  { \n}\n\n/* text style for the selected tab */\n.SelectedTab {\n}\n\n/* hyperlink style for the selected tab */\nA.SelectedTab:link {\n}\n\nA.SelectedTab:visited  {\n}\n\nA.SelectedTab:hover    {\n}\n\nA.SelectedTab:active   {\n}\n\n/* text style for the unselected tabs */\n.OtherTabs {\n}\n    \n/* hyperlink style for the unselected tabs */\nA.OtherTabs:link {\n}\n\nA.OtherTabs:visited  {\n}\n\nA.OtherTabs:hover    {\n}\n\nA.OtherTabs:active   {\n}\n\n/* GENERAL */\n/* style for module titles */\n.Head   {\n}\n\n/* style of item titles on edit and admin pages */\n.SubHead    {\n}\n\n/* module title style used instead of Head for compact rendering by QuickLinks and Signin modules */\n.SubSubHead {\n}\n\n/* text style used for most text rendered by modules */\n.Normal\n{\n}\n\n/* text style used for textboxes in the admin and edit pages, for Nav compatibility */\n.NormalTextBox\n{\n}\n\n.NormalRed\n{\n}\n\n.NormalBold\n{\n}\n\n/* text style for buttons and link buttons used in the portal admin pages */\n.CommandButton     {\n}\n    \n/* hyperlink style for buttons and link buttons used in the portal admin pages */\nA.CommandButton:link {\n}\n\nA.CommandButton:visited  {\n}\n\nA.CommandButton:hover    {\n}\n    \nA.CommandButton:active   {\n}\n\n/* button style for standard HTML buttons */\n.StandardButton     {\n}\n\n/* GENERIC */\nH1  {\n  color: blue;\n}\n\nH2  {\n}\n\nH3  {\n}\n\nH4  {\n}\n\nH5, DT  {\n}\n\nH6  {\n}\n\nTFOOT, THEAD    {\n}\n\nTH  {\n}\n\nA:link  {\n}\n\nA:visited   {\n}\n\nA:hover {\n}\n\nA:active    {\n}\n\nSMALL   {\n}\n\nBIG {\n}\n\nBLOCKQUOTE, PRE {\n}\n\n\nUL LI   {\n}\n\nUL LI LI    {\n}\n\nUL LI LI LI {\n}\n\nOL LI   {\n}\n\nOL OL LI    {\n}\n\nOL OL OL LI {\n}\nOL UL LI   {\n}\n\nHR {\n}\n\n/* MODULE-SPECIFIC */\n/* text style for reading messages in Discussion */    \n.Message    {\n}   \n\n/* style of item titles by Announcements and events */\n.ItemTitle    {\n}\n\n/* Menu-Styles */\n/* Module Title Menu */\n.ModuleTitle_MenuContainer {\n}\n\n.ModuleTitle_MenuBar {\n}\n\n.ModuleTitle_MenuItem {\n}\n\n.ModuleTitle_MenuIcon {\n}\n\n.ModuleTitle_SubMenu {\n}\n\n.ModuleTitle_MenuBreak {\n}\n\n.ModuleTitle_MenuItemSel {\n}\n\n.ModuleTitle_MenuArrow {\n}\n\n.ModuleTitle_RootMenuArrow {\n}\n\n/* Main Menu */\n\n.MainMenu_MenuContainer {\n}\n\n.MainMenu_MenuBar {\n}\n\n.MainMenu_MenuItem {\n}\n\n.MainMenu_MenuIcon {\n}\n\n.MainMenu_SubMenu {\n}\n\n.MainMenu_MenuBreak {\n}\n\n.MainMenu_MenuItemSel {\n}\n\n.MainMenu_MenuArrow {\n}\n\n.MainMenu_RootMenuArrow {\n}\n\n/* Login Styles */\n.LoginPanel{\n}\n\n.LoginTabGroup{\n}\n\n.LoginTab {\n}\n\n.LoginTabSelected{\n}\n\n.LoginTabHover{\n}\n\n.LoginContainerGroup{\n}\n\n.LoginContainer{\n}\n"} """
let [<Literal>] SamplePostDefaultStyleSheet = """ {"portalId":"portalIdReplaceMe"} """
let [<Literal>] SamplePostUpdateConfigFile = """ {
                                           "FileName":"FileNameReplaceMe",
                                           "FileContent":"<?xml version=\"1.0\"?>\r\n<configuration>\r\n  <!-- \r\n\t\tThe blockrequests element contains one or more rules that are used for blocking access to the site.  This filter only works\r\n\t\ton content that is actually processed by ASP.Net.\r\n\t-->\r\n  <blockrequests>\r\n    <!--\r\n\t\t\tEach rule element defines a simple matching expression and the action to take if a match is found.  You can define as many rules\r\n\t\t\tas needed.  This provides a flexible \"or\" operation where any one rule can cause the request to be blocked.\r\n\t\t\tRule Attributes:\r\n\t\t\t~~~~~~~~~~~~~~~~\r\n\t\t\tservervar:  This is the name of a Server variable from the Request.ServerVar hash table.  See: http://www.w3schools.com/asp/coll_servervariables.asp\r\n\t\t    value    :  Defines the value of the servervar that triggers the rule.  For a regex rule, the value should be a regular expression that used as a matching expression. \r\n\t\t\t\t        If this is not a regex operation, then value can be a semicolon delimited list of values.  For example it could include a list of IP addresses that should\r\n\t\t\t\t\t\tbe blocked.\r\n\t\t\toperator :  Defines the operation that determines whether an actual match exists.  Valid values: Regex, Equal, NotEqual\r\n\t\t\t\t\t    >> Regex    : Uses the regular expression specified in the value attribute to determine a match.\r\n\t\t\t\t\t\t>> Equal    : Performs a search of the value list to determine if the value of the specified server variable is in the value list.\r\n\t\t\t\t\t\t>> NotEqual : Performs a search of the value list to determine if the value of the specified server variable does not exist in the value list.\r\n\t\t\taction   :  Defines the action to take if a match occurs.  Valid values: NotFound, Redirect, PermanentRedirect. \r\n\t\t\t\t\t    >> NotFound          : Returns a 404 status code and stops all further response processing.\r\n\t\t\t\t\t\t>> Redirect          : Performs a standard redirect to the url specified in the location attribute.\r\n\t\t\t\t\t\t>> PermanentRedirect : Performs a permanent redirect (status code 301) to the url specified in the location attribute.\r\n\t\t\tlocation :  The url where the request will be redirected.  This can be left blank for the 'NotFound' action.\r\n\t\t<rule servervar=\"URL\" values=\"(?i-msnx:.*default\\.aspx.*)\" operator=\"Regex\" action=\"Redirect\" location=\"http://www.dotnetnuke.com\" />\r\n\t\t<rule servervar=\"HTTPS\" values=\"on\" operator=\"NotEqual\" action=\"NotFound\" location=\"\" />\r\n\t\t<rule servervar=\"REMOTE_ADDR\" values=\"10.10.0.100;192.168.0.100\" operator=\"Equal\" action=\"PermanentRedirect\" location=\"http://www.dotnetnuke.com\" />\r\n\t\t-->\r\n  </blockrequests>\r\n  <skinningdefaults>\r\n    <skininfo folder=\"/Gravity/\" default=\"2-Col.ascx\" admindefault=\"2-Col.ascx\" />\r\n    <containerinfo folder=\"/Gravity/\" default=\"Title_h2.ascx\" admindefault=\"Title_h2.ascx\" />\r\n  </skinningdefaults>\r\n</configuration>"
                                        } """
let [<Literal>] SamplePostMergeConfigFile = """{"fileName":"","fileContent":"<configuration>\n    <nodes configfile=\"FileNameReplaceMe\">\n        <node path=\"/configuration/system.webServer/modules\" action=\"update\" key=\"name\" collision=\"overwrite\">\n            <add name=\"MergeTest\" type=\"DotNetNuke.HttpModules.Compression.CompressionModule, DotNetNuke.HttpModules\" />\n        </node>\n    </nodes>\n</configuration>"}"""
let [<Literal>] SamplePostUpdateSiteMapProvider = """{"Name":"coreSitemapProvider","Description":"Sample description for testing","Enabled":true,"Priority":0.0,"OverridePriority":false}"""
let [<Literal>] SamplePostUpdateSiteMapSettings = """{"SitemapLevelMode":false,"SitemapMinPriority":0.1,"SitemapIncludeHidden":true,"SitemapExcludePriority":0.1,"SitemapCacheDays":0}"""
//let [<Literal>] SamplePostAddLogSettings = """{"LoggingIsActive":true,"LogTypeFriendlyName":"Custom Type","LogTypeKey":"CUSTOM_TYPE","LogTypePortalID":"0","KeepMostRecent":"10","EmailNotificationIsActive":false,"NotificationThreshold":1,"NotificationThresholdTime":1,"NotificationThresholdTimeType":1,"MailFromAddress":"test@QAtesting.com","MailToAddress":"test@QAtesting.com"}"""

let mutable myLoginInfo : UserLoginInfo =     { UserName = ""
                                                Password = ""
                                                DisplayName = ""
                                                DNNCookie = { Name=""; Value="" }
                                                RVCookie = { Name=""; Value="" }
                                                RVToken = { Name=""; Value="" }
                                                }

let mutable myHostLoginInfo : UserLoginInfo =  {UserName = ""
                                                Password = ""
                                                DisplayName = ""
                                                DNNCookie = { Name=""; Value="" }
                                                RVCookie = { Name=""; Value="" }
                                                RVToken = { Name=""; Value="" }
                                                }

let mutable loginInfo : UserLoginInfo = { UserName = ""
                                          Password = ""
                                          DisplayName = ""
                                          DNNCookie = { Name=".DOTNETNUKE"; Value=""}
                                          RVCookie = { Name="__RequestVerificationToken"; Value=""}
                                          RVToken = { Name="RequestVerificationToken"; Value=""}
                                        }

let mutable defaultHostLoginInfo : UserLoginInfo = { UserName = ""
                                                     Password = ""
                                                     DisplayName = ""
                                                     DNNCookie = { Name=".DOTNETNUKE"; Value=""}
                                                     RVCookie = { Name="__RequestVerificationToken"; Value=""}
                                                     RVToken = { Name="RequestVerificationToken"; Value=""}
                                                   }

let mutable defaultAdminLoginInfo : UserLoginInfo = { UserName = ""
                                                      Password = ""
                                                      DisplayName = ""
                                                      DNNCookie = { Name=".DOTNETNUKE"; Value=""}
                                                      RVCookie = { Name="__RequestVerificationToken"; Value=""}
                                                      RVToken = { Name="RequestVerificationToken"; Value=""}
                                                    }

let mutable defaultCMLoginInfo : UserLoginInfo = { UserName = ""
                                                   Password = ""
                                                   DisplayName = ""
                                                   DNNCookie = { Name=".DOTNETNUKE"; Value=""}
                                                   RVCookie = { Name="__RequestVerificationToken"; Value=""}
                                                   RVToken = { Name="RequestVerificationToken"; Value=""}
                                                  }

let mutable defaultCELoginInfo : UserLoginInfo = { UserName = ""
                                                   Password = ""
                                                   DisplayName = ""
                                                   DNNCookie = { Name=".DOTNETNUKE"; Value=""}
                                                   RVCookie = { Name="__RequestVerificationToken"; Value=""}
                                                   RVToken = { Name="RequestVerificationToken"; Value=""}
                                                  }

let mutable defaultMODLoginInfo : UserLoginInfo = { UserName = ""
                                                    Password = ""
                                                    DisplayName = ""
                                                    DNNCookie = { Name=".DOTNETNUKE"; Value=""}
                                                    RVCookie = { Name="__RequestVerificationToken"; Value=""}
                                                    RVToken = { Name="RequestVerificationToken"; Value=""}
                                                  }

let mutable defaultCOMLoginInfo : UserLoginInfo = { UserName = ""
                                                    Password = ""
                                                    DisplayName = ""
                                                    DNNCookie = { Name=".DOTNETNUKE"; Value=""}
                                                    RVCookie = { Name="__RequestVerificationToken"; Value=""}
                                                    RVToken = { Name="RequestVerificationToken"; Value=""}
                                                  }

let mutable defaultRULoginInfo : UserLoginInfo = {    UserName = ""
                                                      Password = ""
                                                      DisplayName = ""
                                                      DNNCookie = { Name=".DOTNETNUKE"; Value=""}
                                                      RVCookie = { Name="__RequestVerificationToken"; Value=""}
                                                      RVToken = { Name="RequestVerificationToken"; Value=""}
                                                    }

let mutable defaultAnonymousLoginInfo : UserLoginInfo = {     UserName = ""
                                                              Password = ""
                                                              DisplayName = ""
                                                              DNNCookie = { Name=".DOTNETNUKE"; Value=""}
                                                              RVCookie = { Name="__RequestVerificationToken"; Value=""}
                                                              RVToken = { Name="RequestVerificationToken"; Value=""}
                                                            }

let [<Literal>] SamplePostAddLogSettings = """{
                                            "KeepMostRecent": "10",
                                            "LogTypeKey": "ADMIN_ALERT",
                                            "LogTypePortalID": "*",
                                            "LoggingIsActive": true,
                                            "EmailNotificationIsActive": true,
                                            "MailFromAddress": "sender@email.com",
                                            "MailToAddress": "receiver@email.com",
                                            "NotificationThreshold": 10,
                                            "NotificationThresholdTime": 10,
                                            "NotificationThresholdTimeType": 3
                                        }"""

let [<Literal>] SamplePostUpdateLogSettings = """{
                                                "ID": "LogSettingIDReplaceMe",
                                                "LoggingIsActive": true,
                                                "LogTypeFriendlyName": "LogTypeFriendlyNameReplaceMe",
                                                "LogTypeKey": "LogTypeKeyReplaceMe",
                                                "LogTypePortalID": "LogTypePortalIDReplaceMe",
                                                "KeepMostRecent": "5",
                                                "EmailNotificationIsActive": true,
                                                "NotificationThreshold": 5,
                                                "NotificationThresholdTime": 5,
                                                "NotificationThresholdTimeType": "3",
                                                "MailFromAddress": "email@emailupdated.com",
                                                "MailToAddress": "email@emailupdated.com"
                                            }"""

let [<Literal>] SamplePostAddLogSettingsTemplate = """{
                                            "KeepMostRecent": "10",
                                            "LogTypeKey": "LogTypeKeyReplaceMe",
                                            "LogTypePortalID": "LogTypePortalIDReplaceMe",
                                            "LoggingIsActive": true,
                                            "EmailNotificationIsActive": true,
                                            "MailFromAddress": "sender@email.com",
                                            "MailToAddress": "receiver@email.com",
                                            "NotificationThreshold": 10,
                                            "NotificationThresholdTime": 10,
                                            "NotificationThresholdTimeType": 3
                                        }"""

let [<Literal>] SamplePostDeleteLogSettings = """{"LogTypeConfigId": "LogSettingIDReplaceMe"}"""
let [<Literal>] SamplePostDeleteLogItems = """[LogItemGuIdsReplaceMe]"""
let [<Literal>] SamplePostEmailLogItems = """
                                            {
                                               "Subject": "Sample",
                                               "Email": "test@dnnsoftware.com",
                                               "Message": "blablabla ...",
                                               "LogIds":[
                                                  LogItemGuIdsReplaceMe
                                               ]
                                            }"""
let [<Literal>] SamplePostPortalCreation = """{
                                               "SiteTemplate":"Blank Website.template|en-US",
                                               "SiteName":"SiteNameReplaceMe",
                                               "SiteAlias":"SiteURLReplaceMe/SiteNameReplaceMe",
                                               "SiteDescription":"SiteDescriptionReplaceMe",
                                               "SiteKeywords":"SiteKeywordsReplaceMe",
                                               "IsChildSite":true,
                                               "HomeDirectory":"Portals/[PortalID]",
                                               "UseCurrentUserAsAdmin":true
                                            }"""

let [<Literal>] SamplePostVocabularyTermCreation = """{
                                               "VocabularyId":VocabularyIdReplaceMe,
                                               "Name":"TermNameReplaceMe",
                                               "Description":"TermDescriptionReplaceMe",
                                               "ParentTermId":ParentTermIdReplaceMe
                                            }"""

let [<Literal>] SamplePostVocabularyTermUpdate = """{
                                               "TermId":TermIdReplaceMe,
                                               "ParentTermId":ParentIdReplaceMe,
                                               "VocabularyId":VocabularyIdReplaceMe,
                                               "Name":"TermNameReplaceMe",
                                               "Description":"TermDescriptionReplaceMe"
                                            }"""

let [<Literal>] SamplePostVocabulary = """{
                                           "Name":"vocabularyNameReplaceMe",
                                           "Description":"DescriptionReplaceMe",
                                           "TypeId":TypeIdReplaceMe,
                                           "ScopeTypeId":ScopeTypeIdReplaceMe
                                        }"""

let [<Literal>] SamplePostCreatePortal  = """{
                                           "SiteTemplate":"SiteTemplateReplaceMe",
                                           "SiteName":"SiteNameReplaceMe",
                                           "SiteAlias":"SiteAliasReplaceMe",
                                           "SiteDescription":"SiteDescriptionReplaceMe",
                                           "SiteKeywords":"SiteKeywordsReplaceMe",
                                           "IsChildSite":true,
                                           "HomeDirectory":"Portals/[PortalID]",
                                           "UseCurrentUserAsAdmin":true
                                        }"""

let [<Literal>] SamplePostVocabularyUpdate = """{
                                            "VocabularyId":vocabularyIdReplaceMe,
                                            "Name":"vocabularyNameReplaceMe",
                                            "Description":"DescriptionChanged",
                                            "Type":"VocTypeReplaceMe",
                                            "TypeId":VocTypeIdReplaceMe,
                                            "ScopeType":"ScopeTypeReplaceMe",
                                            "ScopeTypeId":ScopeTypeIdReplaceMe
                                        }"""

let [<Literal>] SamplePostRolesAddUserToRole = """{"userId":userIDReplaceMe,"roleId":roleIDReplaceMe,"startTime":"2016-08-31T16:00:00.000Z","expiresTime":"0001-01-01T00:00:00"}"""
let [<Literal>] SamplePostRolesCreation = """{"id":-1,"groupId":groupIdReplaceMe,"name":"roleNameReplaceMe","description":"roleDescirptionReplaceMe","serviceFee":0.0,"billingPeriod":0,"billingFrequency":"","trialFee":0.0,"trialPeriod":0,"trialFrequency":"","isPublic":true,"autoAssign":false,"rsvpCode":"","icon":"","status":statusReplaceMe,"securityMode":0,"isSystem":false,"usersCount":0,"allowOwner":false}"""
let [<Literal>] SamplePostPortalSettings = """{"PortalName":"PortalNameReplaceMe","Description":"Sample description","KeyWords":"","GUID":"GUIDReplaceMe","FooterText":"Copyright [year] by DNN Corp","TimeZone":"Pacific Standard Time","HomeDirectory":"HomeDirectoryReplaceMe","LogoFile":"Images/logo.png","FavIcon":"","IconSet":"Sigma"}"""
let [<Literal>] SamplePostExportPortalTemplate = """{
                                                   "FileName":"FileNameReplaceMe",
                                                   "Description":"DescriptionReplaceMe",
                                                   "PortalId":PortalIdRepalceMe,
                                                   "Pages":PagesReplaceMe,
                                                   "Locales":[
                                                      "en-US"
                                                   ],
                                                   "LocalizationCulture":"en-US",
                                                   "IsMultilanguage":false,
                                                   "IncludeContent":true,
                                                   "IncludeFiles":true,
                                                   "IncludeRoles":true,
                                                   "IncludeProfile":true,
                                                   "IncludeModules":true
                                                }"""

let [<Literal>] SamplePostPageDetailsTemplate = """{  "thumbnail": "",
                                                      "workflowId": 1,
                                                      "isWorkflowCompleted": true,
                                                      "applyWorkflowToChildren": false,
                                                      "isWorkflowPropagationAvailable": false,
                                                      "trackLinks": false,
                                                      "defaultThumbnail": "/DesktopModules/admin/Dnn.PersonaBar/Modules/Dnn.Pages/Images/fallback-thumbnail.png",
                                                      "urlThumbnail": "/DesktopModules/admin/Dnn.PersonaBar/Modules/Dnn.Pages/Images/page_link.svg",
                                                      "tabThumbnail": "/DesktopModules/admin/Dnn.PersonaBar/Modules/Dnn.Pages/Images/page_existing.svg",
                                                      "fileThumbnail": "/DesktopModules/admin/Dnn.PersonaBar/Modules/Dnn.Pages/Images/page_file.svg",
                                                      "tabTemplates": tabTemplatesReplaceMe,
                                                      "tabId": 0,
                                                      "name": "pageNameReplaceme",
                                                      "absoluteUrl": null,
                                                      "status": "Visible",
                                                      "localizedName": "",
                                                      "title": "",
                                                      "description": "",
                                                      "keywords": "",
                                                      "tags": "",
                                                      "alias": "",
                                                      "url": "",
                                                      "includeInMenu": true,
                                                      "created": "",
                                                      "createdOnDate": "2017-04-05T01:01:00.000Z",
                                                      "hierarchy": "",
                                                      "hasChild": false,
                                                      "customUrlEnabled": true,
                                                      "pageType": "normal",
                                                      "startDate": null,
                                                      "endDate": null,
                                                      "permissions": permissionsReplaceMe,
                                                      "modules": [],
                                                      "pageUrls": null,
                                                      "isSecure": false,
                                                      "allowIndex": true,
                                                      "cacheProvider": null,
                                                      "cacheDuration": null,
                                                      "cacheIncludeExclude": null,
                                                      "cacheIncludeVaryBy": null,
                                                      "cacheExcludeVaryBy": null,
                                                      "cacheMaxVaryByCount": null,
                                                      "pageHeadText": null,
                                                      "sitemapPriority": 0,
                                                      "permanentRedirect": false,
                                                      "linkNewWindow": false,
                                                      "pageStyleSheet": null,
                                                      "themeName": null,
                                                      "skinSrc": null,
                                                      "containerSrc": null,
                                                      "externalRedirection": "",
                                                      "fileIdRedirection": null,
                                                      "fileNameRedirection": null,
                                                      "fileFolderPathRedirection": null,
                                                      "existingTabRedirection": "",
                                                      "siteAliases": null,
                                                      "primaryAliasId": null,
                                                      "locales": null,
                                                      "hasParent": false,
                                                      "templateTabId": null,
                                                      "templates": templatesReplaceMe,
                                                      "templateId": -1,
                                                      "parentId": null,
                                                      "fileRedirection": "",
                                                      "type": 0,
                                                      "isCopy": false,
                                                      "placeholderURL": "/"
                                                    }"""

let [<Literal>] SamplePostTemmplatesSavePageDetailsTemplate = """{"tabId":0,"name":"templateNameReplaceMe","localizedName":"","title":"","description":"templateDescriptionReplaceMe","tags":"","keywords":"","alias":"","url":"/","includeInMenu":true,"thumbnail":"","created":"","hierarchy":"","hasChild":false,"type":0,"customUrlEnabled":true,"templateId":templateIdReplaceMe,"pageType":"template","workflowId":1,"isWorkflowCompleted":false,"applyWorkflowToChildren":false,"isWorkflowPropagationAvailable":false,"isCopy":false,"trackLinks":false,"errors":[]}"""

let [<Literal>] MySqlScriptViewPageListDefaultPermission = """select * from [PersonaBarMenuDefaultPermissions] Where RoleNames like '%IdentifierReplaceMe%'"""

let [<Literal>] MySqlScript =
                       """ DECLARE @Identifier NVARCHAR(50) = 'IdentifierReplaceMe'    -- the menu identifier, make sure its parent identifier have view permission for new role.
                            DECLARE @RoleId INT = RoleIdReplaceMe                                -- the role id
                            DECLARE @PermissionKey NVARCHAR(50) = 'PermissionKeyReplaceMe'       -- the permission key, can be VIEW/EDIT.
                            DECLARE @PortalId INT = PortalIdReplaceMe                            -- Portal Id.
                            DECLARE @AllowAccess BIT = AllowAccessReplaceMe                      -- 1 - Allow, 0 - Fobidden.

                            DECLARE @MenuId INT
                            DECLARE @ParentId INT
                            DECLARE @PermissionId INT

                            SELECT @MenuId = MenuId, @ParentId = ParentId FROM dbo.PersonaBarMenu WHERE Identifier = @Identifier
                            SELECT @PermissionId = PermissionId FROM dbo.PersonaBarPermission WHERE PermissionKey = @PermissionKey

                            IF NOT EXISTS(SELECT * FROM dbo.PersonaBarMenuPermission WHERE RoleId = @RoleId AND MenuId = @MenuId AND PermissionId = @PermissionId AND PortalId = @PortalId)
                            BEGIN
                                INSERT INTO dbo.PersonaBarMenuPermission
                                        ( PortalId ,
                                          MenuId ,
                                          PermissionId ,
                                          AllowAccess ,
                                          RoleId ,
                                          UserId ,
                                          CreatedByUserId ,
                                          CreatedOnDate ,
                                          LastModifiedByUserId ,
                                          LastModifiedOnDate
                                        )
                                VALUES  ( @PortalId , -- PortalId - int
                                          @MenuId , -- MenuId - int
                                          @PermissionId , -- PermissionId - int
                                          @AllowAccess , -- AllowAccess - bit
                                          @RoleId , -- RoleId - int
                                          NULL , -- UserId - int
                                          NULL , -- CreatedByUserId - int
                                          GETDATE() , -- CreatedOnDate - datetime
                                          NULL , -- LastModifiedByUserId - int
                                          GETDATE()  -- LastModifiedOnDate - datetime
                                        )
                            END
                            ELSE
                            BEGIN
                                UPDATE dbo.PersonaBarMenuPermission
                                    SET AllowAccess = @AllowAccess
                                    WHERE RoleId = @RoleId AND MenuId = @MenuId AND PermissionId = @PermissionId AND PortalId = @PortalId
                            END

                            IF (@ParentId Is Not Null)
                            BEGIN
                                SET @MenuId = @ParentId
                                IF NOT EXISTS(SELECT * FROM dbo.PersonaBarMenuPermission WHERE RoleId = @RoleId AND MenuId = @MenuId AND PermissionId = @PermissionId AND PortalId = @PortalId)
                                BEGIN
                                    INSERT INTO dbo.PersonaBarMenuPermission
                                            ( PortalId ,
                                              MenuId ,
                                              PermissionId ,
                                              AllowAccess ,
                                              RoleId ,
                                              UserId ,
                                              CreatedByUserId ,
                                              CreatedOnDate ,
                                              LastModifiedByUserId ,
                                              LastModifiedOnDate
                                            )
                                    VALUES  ( @PortalId , -- PortalId - int
                                              @MenuId , -- MenuId - int
                                              @PermissionId , -- PermissionId - int
                                              @AllowAccess , -- AllowAccess - bit
                                              @RoleId , -- RoleId - int
                                              NULL , -- UserId - int
                                              NULL , -- CreatedByUserId - int
                                              GETDATE() , -- CreatedOnDate - datetime
                                              NULL , -- LastModifiedByUserId - int
                                              GETDATE()  -- LastModifiedOnDate - datetime
                                            )
                                END
                                ELSE
                                BEGIN
                                    UPDATE dbo.PersonaBarMenuPermission
                                        SET AllowAccess = @AllowAccess
                                        WHERE RoleId = @RoleId AND MenuId = @MenuId AND PermissionId = @PermissionId AND PortalId = @PortalId
                                END
                            END"""
