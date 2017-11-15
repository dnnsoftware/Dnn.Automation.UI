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
module DnnConstants

open System.Configuration

// These licenses will work 1000 times only. Please contact support to renew these awhen they are all consumed.

let [<Literal>] LoginCookieName = ".DOTNETNUKE"

let [<Literal>] DefaultUserAgent =
    "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/50.0.2661.87 Safari/537.36"

let loremIpsumText = 
    "Lorem ipsum dolor sit amet, convallis ut a faucibus tincidunt sed massa, metus viverra leo, amet aliquam ac ligula." +
    " Erat mus venenatis urna, malesuada est iaculis morbi vivamus, ullamcorper blandit nullam quis elit est. Euismod molestias massa qua" +
    "m vivamus vehicula. Tincidunt ultricies nullam adipiscing, fusce sit sapien in erat elit, ultrices elit, a vivamus sed nec vel plate" +
    "a, libero mauris nam cras id. Et lacinia auctor cursus tellus ornare, faucibus nemo suscipit, semper suspendisse tempus suspendisse " +
    "duis dui lacus, massa enim. Vivamus odio suscipit nulla. Odio turpis ac pulvinar, leo vivamus semper sit lacus, lectus natoque aliqu" +
    "am dolor. Odio interdum, nostrud ridiculus nam rutrum ligula, velit at sagittis venenatis. Ante maecenas id dui pellentesque nostra." +
    " Dolor a, accumsan dui. Condimentum convallis non pellentesque accumsan, nulla leo nulla suscipit suscipit id, nibh per, dictum matt" +
    "is ut neque curabitur, aliquam sed venenatis non tortor. Turpis mattis nulla montes, elit augue vivamus eu netus, ut sed vestibulum " +
    "dolor, dui at sed interdum wisi, sollicitudin aliquam. Penatibus at morbi consequat adipiscing dui consequat, mauris orci ante id no" +
    "stra faucibus a, nullam nonummy a sem parturient. Non non nibh sed sodales ullamcorper elementum. Eu magnis nullam massa. Vel mauris" +
    " ornare, eget metus quis, vestibulum ut ipsum nam leo velit, tortor ligula nulla in ut sed ac. Adipiscing dolores nonummy, id proide" +
    "nt purus est amet hendrerit. Sapien in tortor sed, viverra hendrerit nulla. Excepteur tincidunt, id ligula donec orci lobortis, leo " +
    "nec quis orci etiam. Justo eget ornare mauris euismod neque etiam, quam vitae tortor morbi donec, nec non nonummy, scelerisque eget " +
    "convallis eu torquent non. Duis condimentum pellentesque, eu ipsum suspendisse integer, exercitation sem aliquet ut consectetuer hym" +
    "enaeos arcu, tempor urna habitant ut, tristique sem justo sem eget tortor. Et convallis in rhoncus. Magna ridiculus fermentum ultric" +
    "es dolor et morbi, libero urna et nec mi, proin a, hendrerit arcu scelerisque risus. Vehicula at pretium, quam gravida elit, digniss" +
    "im tortor sem quis dui, luctus et arcu, convallis duis voluptatem placerat vestibulum montes. Vestibulum laoreet eget, adipiscing cr" +
    "as ante, pede ut amet suspendisse, viverra netus mollis consectetuer quis placerat. Consequat maecenas lacus, egestas nullam eros. E" +
    "nim volutpat, mollis neque, est tempor diam, integer pellentesque phasellus mollis lacinia nibh nulla, et sit. Porttitor maecenas ne" +
    "c adipiscing tellus pulvinar, sit amet placerat. Suscipit elementum, laboris fusce odio arcu at, lacinia pede vestibulum. Pretium tu" +
    "rpis molestie mollis diam scelerisque, nec nunc ac laoreet commodo erat urna. Metus tellus in nisl pulvinar scelerisque, in odio odi" +
    "o nam fringilla justo, ut congue dolor lacus magna sed fuga, cras dolor lectus dolor purus libero. Aptent a tellus eu ipsum mauris d" +
    "iam. Commodo diam tortor ultricies nunc, nullam auctor duis habitant enim faucibus, elementum mi ligula orci volutpat mollis vestibu" +
    "lum, convallis sed duis in ornare, eros vitae id auctor amet at amet. Pellentesque mi sem in torquent purus dictum, massa vel praese" +
    "nt leo quisque, lobortis morbi in, leo cursus morbi ante sollicitudin pariatur. In odio lacus, interdum quis tincidunt velit in volu" +
    "tpat consequat. Consequatur vivamus donec felis, lorem orci, amet neque. Vulputate sed convallis vel tellus in, ut quam. Interdum au" +
    "gue risus porttitor, vitae et, porta ut pellentesque. Placerat felis sapien ut. Neque integer pharetra, bibendum augue id maecenas. " +
    "Enim pharetra diam nam, et amet eget eget quisque id eget. Aliquam ante qui, porttitor convallis id ad, nec vestibulum tempor amet t" +
    "ortor lacus massa, fusce torquent. Sodales tortor suspendisse in ligula molestie, mauris wisi bibendum elit quam interdum, in auctor" +
    " in ullamcorper sed dui sed, metus nunc elit, viverra ante adipiscing vehicula imperdiet pede ut. Pellentesque natoque ut tellus, ma" +
    "uris eget. Nulla venenatis dui pede amet, dui diam eu diam, ipsum curabitur ac leo duis turpis fermentum, suspendisse sit quam."

//engineering
let longUserName = 
    "MohitAmritBingKenBehzadGeorgeKanAshishAmarjitMiguelPavelFrancescoPedroAntonioFranJuan" +
    "MohitAmritBingKenBehzadGeorgeKanAshishAmarjitMiguelPavelFrancescoPedroAntonioFranJuan" +
    "MohitAmritBingKenBehzadGeorgeKanAshishAmarjitMiguelPavelFrancescoPedroAntonioFranJuan" +
    "MohitAmritBingKenBehzadGeorgeKanAshishAmarjitMiguelPavelFrancescoPedroAntonioFranJuan"

//greek alphabet
let longPassword =
    "alphabetagammadeltaepsilonzetaetathetaiotakappalambdamunuxiomicronpirhosigmatauupsilonphichipsiomega"

let azureAccountName = ConfigurationManager.AppSettings.["azureAccountName"]
let azureAccountKey = ConfigurationManager.AppSettings.["azureAccountKey"]

let testHtmlCode = 
    """<div class="header-image-960"><div class="header-copy">
        <h1><span class="Story-Header">The World’s Finest AV Products</span></h1>
        <span class="Story-Body" style="margin-bottom:25px;">Since 1953, when it began producing widgets, 
        the&nbsp;Cavalier Corporation in Japan (then Karloki Buto Co., Ltd.) has grown to become the world's 
        largest manufacturer of a full line of televisions, and a leading producer of&nbsp;audio/visual products.</span>
        <a href="/Our-Products">
        <div class="CTA-btn">Learn More
        </div></a></div>
    <div class="header-image">
        <img src="/portals/0/images/cavalier-tv.png" style="max-width: 100%;" alt="Cavalier TV">
    </div></div>"""

let mutable PBPermissionRead = "0" //Initially, no permission
let mutable PBPermissionEdit = "0" //Initially, no permission
let mutable PBPermissionUsersView = "0"
let mutable PBPermissionUsersEdit = "0"
let mutable PBPermissionAdminLogView = "0"
let mutable PBPermissionAdminLogEdit = "0"
let mutable PBPermissionRecycleBinView = "0"
let mutable PBPermissionRecycleBinEdit = "0"
let mutable PBPermissionSecurityView = "0"
let mutable PBPermissionSecurityEdit = "0"
let mutable PBPermissionSiteInfoView = "0" //Initially, no permission
let mutable PBPermissionSiteInfoEdit = "0" //Initially, no permission

let PBIdentifierNames = [ 
                        "Dnn.AdminLogs"; 
                        "Dnn.Recyclebin";
                        "Dnn.Roles";
                        "Dnn.Security";
                        "Dnn.Users";
                        "Dnn.Vocabularies";
                        "Dnn.Pages";
                        ]

let PBIdentifierNamesForSiteInfo = ["Dnn.SiteSettings"]
let PBIdentifierNamesForUsersView = [
                                    "SHOW_USER_ACTIVITY";
                                    "VIEW_ASSETS";
                                    "VIEW_SETTINGS"
                                    ]
let PBIdentifierNamesForUsersEdit = ["ADD_USER";
                                    "AUTHORIZE_UNAUTHORIZE_USER";
                                    "DELETE_USER";
                                    "EDIT_POINTS";
                                    "EDIT_SETTINGS";
                                    "LOGIN_AS_USER";
                                    "MANAGE_PASSWORD";
                                    "MANAGE_PROFILE";
                                    "MANAGE_ROLES"
                                    ]
let PBIdentifierNamesForAdminLogView = [
                                        "ADMIN_LOGS_VIEW";
                                        "LOG_SETTINGS_VIEW"
                                       ]
let PBIdentifierNamesForAdminLogEdit = [
                                        "ADMIN_LOGS_EDIT";
                                        "LOG_SETTINGS_EDIT"
                                       ]

let PBIdentifierNamesForRecycleBinEdit = [
                                          "RECYCLEBIN_MODULES_EDIT";
                                          "RECYCLEBIN_PAGES_EDIT";
                                          "RECYCLEBIN_TEMPLATES_EDIT";
                                          "RECYCLEBIN_USERS_EDIT";
                                         ]

let PBIdentifierNamesForRecycleBinView = [
                                          "RECYCLEBIN_MODULES_VIEW";
                                          "RECYCLEBIN_PAGES_VIEW";
                                          "RECYCLEBIN_TEMPLATES_VIEW";
                                          "RECYCLEBIN_USERS_VIEW";
                                          "LOG_SETTINGS_VIEW";
                                         ]

let PBIdentifierNamesForSecurityEdit = [
                                        "BASIC_LOGIN_SETTINGS_EDIT";
                                        "REGISTRATION_SETTINGS_EDIT"
                                         ]

let PBIdentifierNamesForSecurityView = [
                                        "BASIC_LOGIN_SETTINGS_VIEW";
                                        "REGISTRATION_SETTINGS_VIEW"
                                       ]