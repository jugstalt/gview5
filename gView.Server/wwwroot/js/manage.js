if (!window.gview) window.gview = {};

window.gview.manage = (function() {
    var rootUrl = '/';
    const UrlTokenPrefix = 'token-';
    const UrlTokenSplitter = '~';

    var setRootUrl = function(url) {
        rootUrl = url;
    };

    //
    //  Get/Post
    //

    var get = function(options) {
        $.ajax({
            url: rootUrl + options.url,
            type: options.type || 'get',
            data: options.data || null,
            success:
                options.success ||
                function(result) {
                    alert(result);
                },
            error:
                options.error ||
                function(jqXHR, textStatus, errorThrown) {
                    $('.loading').removeClass('loading');
                    alert('Error: ' + errorThrown + '(' + textStatus + ')');
                }
        });
    };

    var postForm = function($form, options) {
        var data = {};

        if ($form.find('.form-value').length === 0) {
            data['_emtpyform'] = true; // One properterty needed!!!!
        } else {
            $form.find('.form-value').each(function(i, input) {
                data[$(input).attr('name')] =
                    $(input).attr('type') === 'checkbox'
                        ? $(input).prop('checked')
                        : $(input).val();
            });
        }

        options.type = 'post';
        options.data = data;

        var onSuccess = options.success;
        options.success = function(result) {
            $form.find('.form-error').remove();

            if (!result.success) {
                $('<div>')
                    .addClass('form-error')
                    .html(result.error)
                    .prependTo($form);
            } else {
                if (onSuccess) onSuccess(result);
            }
        };
        get(options);
    };

    //
    // UI Elements
    //

    var modalDialog = function (options) {
        var $blocker = $('<div>')
            .addClass('gview5-modal-blocker')
            .appendTo($('body'))
            .click(function (e) {
                //$(this)
                //    .find('.button-close')
                //    .trigger('click');
            });

        var $modal = $('<div>')
            .addClass('modal-dialog')
            .appendTo($blocker)
            .click(function(e) {
                e.stopPropagation();
            });

        if (options.title) {
            $('<div>' + options.title + '</div>')
                .addClass('modal-title')
                .appendTo($modal);
        }
        var $body = $('<div>')
            .addClass('modal-body')
            .appendTo($modal);
        var $footer = $('<div>')
            .addClass('modal-footer')
            .appendTo($modal);

        $('<button>Close</button>')
            .addClass('button-close')
            .appendTo($footer)
            .click(function(e) {
                e.stopPropagation();
                if (options.onClose) options.onClose($body);
                $(this)
                    .closest('.gview5-modal-blocker')
                    .remove();
            });
        if (options.onOk) {
            $('<button>OK</button>')
                .addClass('button-ok')
                .appendTo($footer)
                .click(function(e) {
                    e.stopPropagation();

                    var autoClose = options.autoClose === false ? false : true;
                    if (options.onOk) {
                        var ret = options.onOk($body);
                        autoClose = autoClose || ret;
                    }
                    if (autoClose === true) {
                        $(this)
                            .closest('.gview5-modal-blocker')
                            .remove();
                    }
                });
        }

        if (options.onLoad) options.onLoad($body);
    };

    //
    // Page Services
    //
    var createServiceListItem = function($services, service) {
        //console.log(service);
        var $service = $services.children(
            ".service[data-service='" +
                (service.folder ? service.folder + '/' : '') +
                service.name +
                "']"
        );
        if ($service.length === 0) {
            $service = $('<li></li>')
                .attr('data-service', (service.folder ? service.folder + '/' : '') + service.name)
                .addClass('service')
                .appendTo($services);
        }
        $service
            .removeClass()
            .addClass('service ' + service.status)
            .empty();
        if (service.hasErrors) $service.addClass('has-errors');

        var $toolbar = $('<div>')
            .addClass('toolbar')
            .appendTo($service);

        $('<div>')
            .addClass('icon clickable log')
            .appendTo($toolbar)
            .click(function() {
                var serviceName = $(this)
                    .closest('.service')
                    .attr('data-service');

                var renderErrorLogs = function($target, result) {
                    $target.empty();
                    var $ul = $('<ul>').appendTo($target);
                    $.each(result.errors, function(i, e) {
                        $('<li>')
                            .appendTo($ul)
                            .html(e.replace(/(?:\r\n|\r|\n)/g, '<br>'));
                    });
                    if (result.ticks) {
                        $('<button>older...</button>')
                            .appendTo($target)
                            .click(function() {
                                get({
                                    url:
                                        '/manage/serviceerrorlogs?service=' +
                                        serviceName +
                                        '&last=' +
                                        result.ticks,
                                    success: function(result) {
                                        renderErrorLogs($target, result);
                                    }
                                });
                            });
                    }
                };

                modalDialog({
                    title: service.name + ' (Error Logs)',
                    onLoad: function($body) {
                        $body.addClass('error-logs');
                        get({
                            url: '/manage/serviceerrorlogs?service=' + serviceName,
                            success: function(result) {
                                renderErrorLogs($body, result);
                            }
                        });
                    }
                });
            });
        $('<div>')
            .addClass('icon clickable security' + (service.hasSecurity === true ? '1' : '0'))
            .appendTo($toolbar)
            .click(function() {
                var serviceName = $(this)
                    .closest('.service')
                    .attr('data-service');

                var renderSecurityTableRow = function($row, allTypes, rule) {
                    $('<td>✖</td>')
                        .addClass('remove')
                        .appendTo($row)
                        .click(function() {
                            $(this)
                                .closest('tr')
                                .remove();
                        });
                    $('<td>')
                        .addClass('username')
                        .attr('data-username', rule.username)
                        .html(rule.username)
                        .appendTo($row);

                    var allInterpreters = false;
                    $.each(allTypes, function(i, type) {
                        $cell = $('<td>')
                            .addClass('rule')
                            .appendTo($row);
                        var hasRule = $.inArray(type.toLowerCase(), rule.servicetypes) >= 0;
                        var $chkbox = $(
                            "<input type='checkbox' name='" + rule.username + '~' + type + "' />"
                        )
                            .addClass('form-value')
                            .prop('checked', hasRule)
                            .appendTo($cell);

                        if (type.indexOf('_') === 0) {
                            if (type === '_all') {
                                allInterpreters = hasRule;
                                $chkbox.click(function() {
                                    var val = $(this).prop('checked');
                                    $(this)
                                        .closest('tr')
                                        .find('input.interpreter')
                                        .css('display', val === true ? 'none' : '');
                                });
                            } else {
                                $chkbox
                                    .addClass('interpreter')
                                    .css('display', allInterpreters === true ? 'none' : '');
                            }
                        }
                    });
                };
                var renderSecurityTable = function($target, result) {
                    $target.empty();

                    var $tab = $('<table>').appendTo($target);
                    var $row = $('<tr>').appendTo($tab);

                    var $cell = $('<th>').appendTo($row); // remove (X)
                    $('<th>')
                        .html('User')
                        .appendTo($row);

                    $.each(result.allTypes, function(i, type) {
                        var title = toUIString(type);
                        if (title.indexOf('_') === 0) title = '#' + title.substr(1).toUpperCase();
                        $('<th>')
                            .addClass('rule')
                            .html(title)
                            .appendTo($row);
                    });

                    $.each(result.accessRules, function(i, rule) {
                        $row = $('<tr>').appendTo($tab);
                        renderSecurityTableRow($row, result.allTypes, rule);
                    });

                    $row = $('<tr>').appendTo($tab);
                    $('<td>').appendTo($row); // remove (X)
                    $cell = $('<td>').appendTo($row);

                    var $selectUser = $('<select>').appendTo($cell);
                    $("<option value=''></options>").appendTo($selectUser);
                    $(
                        "<option value='" +
                            result.anonymousUsername +
                            "'>" +
                            result.anonymousUsername +
                            '</option>'
                    ).appendTo($selectUser);
                    $.each(result.allUsers, function(i, user) {
                        $("<option value='" + user + "'>")
                            .html(user)
                            .appendTo($selectUser);
                    });
                    $selectUser.click(function() {
                        val = $(this).val();
                        if (
                            val &&
                            $(this)
                                .closest('table')
                                .find(".username[data-username='" + val + "']").length === 0
                        ) {
                            $row = $('<tr>').insertBefore($(this).closest('tr'));

                            var servicetypes = [];
                            $.each(result.allTypes, function(i, t) {
                                if (t.indexOf('_') !== 0 || t === '_all')
                                    servicetypes.push(t.toLowerCase());
                            });
                            renderSecurityTableRow($row, result.allTypes, {
                                username: val,
                                servicetypes: servicetypes
                            });
                        }
                        $(this).val('');
                    });
                };

                modalDialog({
                    title: service.name + ' (Security)',
                    onLoad: function($body) {
                        $body.addClass('service-security');
                        get({
                            url: '/manage/servicesecurity?service=' + serviceName,
                            success: function(result) {
                                renderSecurityTable($body, result);
                            }
                        });
                    },
                    onOk: function($body) {
                        postForm($body, {
                            url: '/manage/servicesecurity?service=' + serviceName,
                            success: function(result) {
                                console.log(result);
                                createServiceListItem(
                                    $('.gview-manage-body').find('.services'),
                                    result.service
                                );
                            }
                        });
                    }
                });
            });
        $('<div>')
            .addClass('icon clickable start ' + (service.status === 'running' ? 'gray' : ''))
            .appendTo($toolbar)
            .click(function() {
                setServiceStatus(this, $(this).closest('.service'), 'running');
            });
        $('<div>')
            .addClass('icon clickable stop ' + (service.status === 'stopped' ? 'gray' : ''))
            .appendTo($toolbar)
            .click(function() {
                setServiceStatus(this, $(this).closest('.service'), 'stopped');
            });
        $('<div>')
            .addClass('icon clickable settings')
            .appendTo($toolbar)
            .click(function () {
                let serviceName = $(this)
                    .closest('.service')
                    .attr('data-service');

                let metadata = [];
                let editor = null, currentSelected = null;
                modalDialog({
                    title: service.name + ' (Metadata)',
                    autoClose: false,
                    onLoad: function ($body) {
                        $body.addClass('service-security');
                        get({
                            url: '/manage/servicemetadata?service=' + serviceName,
                            success: function (result) {
                                metadata = result;

                                let $select = $("<select>")
                                    .css({ width: '100%', height: 25 })
                                    .appendTo($body);
                                for (var id in result) {
                                    $("<option>")
                                        .attr('value', id)
                                        .text(id)
                                        .appendTo($select);
                                }

                                let $editor = $("<div>")
                                    .css({ width: '100%', height: 'calc(100% - 30px)' })
                                    .appendTo($body);

                                editor = monaco.editor.create($editor.get(0), {
                                    language: 'yaml',
                                    automaticLayout: true,
                                    contextmenu: false,
                                    theme: 'vs-dark'
                                });

                                $select.change(function () {
                                    if (currentSelected) {
                                        metadata[currentSelected] = editor.getValue();
                                    }
                                    currentSelected = $(this).val();
                                    editor.setValue(metadata[currentSelected]);
                                });
                                $select.trigger('change');
                            }
                        });
                    },
                    onOk: function ($body) {
                        if (editor && currentSelected) { // sumit current
                            metadata[currentSelected] = editor.getValue();
                        }
                        get({
                            url: '/manage/servicemetadata',
                            type: 'post',
                            data: {
                                service: serviceName,
                                metadata: metadata
                            },
                            success: function (result) {
                                if (!result.success) {
                                    alert(result.error);
                                } else {
                                    $('.gview5-modal-blocker').remove();
                                }
                            }
                        });

                        return false;
                    }
                });
            });
        $('<div>')
            .addClass('icon clickable refresh')
            .appendTo($toolbar)
            .click(function() {
                setServiceStatus(this, $(this).closest('.service'), 'refresh');
            });

        $('<h4>' + service.name + '</h4>').appendTo($service);
        if (service.runningSince) {
            $('<div>Running since: ' + service.runningSince + '</div>').appendTo($service);
        }
        return $service;
    };

    var folderServices = function(folder) {
        var $services = $('.services').empty();
        get({
            url: '/manage/services?folder=' + folder,
            success: function(result) {
                $.each(result.services, function(i, service) {
                    createServiceListItem($services, service);
                });
            }
        });
    };

    var pageServices = function() {
        var $body = $('.gview-manage-body')
            .empty()
            .addClass('loading');

        get({
            url: '/manage/folders',
            success: function(result) {
                $body.removeClass('loading');

                var $container = $("<div>")
                    .addClass('container')
                    .css('max-height','unset')
                    .appendTo($body)

                var $folders = $('<ul>')
                    .addClass('folders')
                    .appendTo($container);
                $('<li>')
                    .addClass('folder selected')
                    .html('(root)')
                    .attr('data-folder', '')
                    .appendTo($folders);
                $.each(result.folders, function(i, folder) {
                    if (folder) {
                        var $folder = $('<li>')
                            .addClass('folder')
                            .html(folder.name)
                            .attr('data-folder', folder.name)
                            .appendTo($folders);

                        var $toolbar = $('<div>')
                            .addClass('toolbar')
                            .appendTo($folder);
                        $('<div>')
                            .addClass(
                                'icon clickable security' +
                                    (folder.hasSecurity === true ? '1' : '0')
                            )
                            .appendTo($toolbar)
                            .click(function(e) {
                                e.stopPropagation();

                                var renderSecurityTableRow = function ($row, allTypes, rule) {

                                    var username = rule.username.indexOf(UrlTokenPrefix) === 0
                                        ? rule.username.split(UrlTokenSplitter)[0]
                                        : rule.username;

                                    $('<td>✖</td>')
                                        .addClass('remove')
                                        .appendTo($row)
                                        .click(function() {
                                            $(this)
                                                .closest('tr')
                                                .remove();
                                        });
                                    $('<td>')
                                        .addClass('username')
                                        .attr('data-username', username)
                                        .html(username)
                                        .appendTo($row);

                                    var allInterpreters = false;
                                    $.each(allTypes, function(i, type) {
                                        $cell = $('<td>')
                                            .addClass('rule')
                                            .appendTo($row);
                                        var hasRule =
                                            $.inArray(type.toLowerCase(), rule.servicetypes) >= 0;
                                        var $chkbox = $(
                                            "<input type='checkbox' name='" +
                                                rule.username +
                                                '~' +
                                                type +
                                                "' />"
                                        )
                                            .addClass('form-value')
                                            .prop('checked', hasRule)
                                            .appendTo($cell);

                                        if (type.indexOf('_') === 0) {
                                            if (type === '_all') {
                                                allInterpreters = hasRule;
                                                $chkbox.click(function() {
                                                    var val = $(this).prop('checked');
                                                    $(this)
                                                        .closest('tr')
                                                        .find('input.interpreter')
                                                        .css('display', val === true ? 'none' : '');
                                                });
                                            } else {
                                                $chkbox
                                                    .addClass('interpreter')
                                                    .css(
                                                        'display',
                                                        allInterpreters === true ? 'none' : ''
                                                    );
                                            }
                                        }
                                    });
                                };

                                var renderSecurityTable = function(
                                    $target,
                                    result,
                                    advancedSettings
                                ) {
                                    $target.empty();

                                    var $tab = $('<table>').appendTo($target);
                                    var $row = $('<tr>').appendTo($tab);

                                    var $cell = $('<th>').appendTo($row); // remove (X)
                                    $('<th>')
                                        .html('User')
                                        .appendTo($row);

                                    $.each(result.allTypes, function(i, type) {
                                        var title = toUIString(type);
                                        if (title.indexOf('_') === 0)
                                            title = '#' + title.substr(1).toUpperCase();
                                        $('<th>')
                                            .addClass('rule')
                                            .html(title)
                                            .appendTo($row);
                                    });

                                    $.each(result.accessRules, function(i, rule) {
                                        $row = $('<tr>').appendTo($tab);
                                        renderSecurityTableRow($row, result.allTypes, rule);
                                    });

                                    $row = $('<tr>').appendTo($tab);
                                    $('<td>').appendTo($row); // remove (X)
                                    $cell = $('<td>').appendTo($row);

                                    var $selectUser = $('<select>').appendTo($cell);
                                    $("<option value=''></options>").appendTo($selectUser);
                                    $(
                                        "<option value='" +
                                            result.anonymousUsername +
                                            "'>" +
                                            result.anonymousUsername +
                                            '</option>'
                                    ).appendTo($selectUser);
                                    $.each(result.allUsers, function (i, user) {
                                        var username = user.indexOf(UrlTokenPrefix) === 0
                                            ? user.split(UrlTokenSplitter)[0]
                                            : user;

                                        $("<option value='" + username + "'>")
                                            .html(username)
                                            .appendTo($selectUser);
                                    });
                                    $selectUser.click(function() {
                                        val = $(this).val();
                                        if (
                                            val &&
                                            $(this)
                                                .closest('table')
                                                .find(".username[data-username='" + val + "']")
                                                .length === 0
                                        ) {
                                            $row = $('<tr>').insertBefore($(this).closest('tr'));

                                            var servicetypes = [];
                                            $.each(result.allTypes, function(i, t) {
                                                if (t.indexOf('_') !== 0 || t === '_all')
                                                    servicetypes.push(t.toLowerCase());
                                            });
                                            renderSecurityTableRow($row, result.allTypes, {
                                                username: val,
                                                servicetypes: servicetypes
                                            });
                                        }
                                        $(this).val('');
                                    });

                                    if (advancedSettings) {
                                        var $settingsDiv = $('<div>')
                                            .addClass('section')
                                            .appendTo($target);
                                        $('<h4>Folder Settings</h4>').appendTo($settingsDiv);

                                        var $formInput = $('<div>')
                                            .addClass('form-input')
                                            .appendTo($settingsDiv);

                                        $('<div>')
                                            .addClass('label')
                                            .text('Online Resource (override)')
                                            .appendTo($formInput);
                                        $('<br>').appendTo($formInput);
                                        $("<input name='advancedsettings_onlineresource'>")
                                            .addClass('form-value')
                                            .val(result.onlineResource)
                                            .appendTo($formInput);

                                        $formInput = $('<div>')
                                            .addClass('form-input')
                                            .appendTo($settingsDiv);
                                        $('<div>')
                                            .addClass('label')
                                            .text('Output Url (override)')
                                            .appendTo($formInput);
                                        $('<br>').appendTo($formInput);
                                        $("<input name='advancedsettings_outputurl'>")
                                            .addClass('form-value')
                                            .val(result.outputUrl)
                                            .appendTo($formInput);
                                    }
                                };

                                modalDialog({
                                    title: folder.name + ' (Security)',
                                    onLoad: function($body) {
                                        $body.addClass('service-security');
                                        get({
                                            url: '/manage/foldersecurity?folder=' + folder.name,
                                            success: function(result) {
                                                renderSecurityTable($body, result, true);
                                            }
                                        });
                                    },
                                    onOk: function($body) {
                                        postForm($body, {
                                            url: '/manage/foldersecurity?folder=' + folder.name,
                                            success: function(result) {
                                                if (result && result.success && result.folder) {
                                                    var $folder = $(
                                                        ".folders .folder[data-folder='" +
                                                            result.folder.name +
                                                            "']"
                                                    );
                                                    var cls =
                                                        'security' +
                                                        (result.folder.hasSecurity === true
                                                            ? '1'
                                                            : '0');
                                                    if (
                                                        $folder.find('.icon.clickable.security1')
                                                            .length > 0
                                                    ) {
                                                        $folder
                                                            .find('.icon.clickable.security1')
                                                            .removeClass('security1')
                                                            .addClass(cls);
                                                    } else if (
                                                        $folder.find('.icon.clickable.security0')
                                                            .length > 0
                                                    ) {
                                                        $folder
                                                            .find('.icon.clickable.security0')
                                                            .removeClass('security0')
                                                            .addClass(cls);
                                                    }
                                                }
                                            }
                                        });
                                    }
                                });
                            });
                    }
                });
                $folders.children('.folder').click(function() {
                    $(this)
                        .parent()
                        .children('.folder')
                        .removeClass('selected');
                    folderServices(
                        $(this)
                            .addClass('selected')
                            .attr('data-folder')
                    );
                });

                var $services = $('<ul>')
                    .addClass('services')
                    .appendTo($container);
                folderServices('');
                //$.each(result.services, function (i, service) {
                //    createServiceListItem($services, service);
                //});
            }
        });
    };

    var setServiceStatus = function (sender, $service, status) {
        $(sender).addClass('loading');

        get({
            url:
                '/manage/setservicestatus?service=' +
                $service.attr('data-service') +
                '&status=' +
                status,
            type: 'post',
            success: function (result) {
                $(sender).removeClass('loading');
                createServiceListItem($service.closest('.services'), result.service);
            }
        });
    };

    //
    // Page Security
    //
    var appendFormInput = function($form, name, type, label, readonly, val) {
        var $formInput = $('<div>')
            .addClass('form-input')
            .appendTo($form);
        $('<div>')
            .addClass('label')
            .html(label || name)
            .appendTo($formInput);
        $('<br/>').appendTo($formInput);

        var $input = $("<input name='" + name + "' type='" + (type || 'text') + "' autocomplete='off' />")
            .addClass('form-value')
            .appendTo($formInput);

        if (readonly === true) {
            $input.attr('readonly', 'readonly');
        }
        if (val) {
            $input.val(val);
        }
    };
    var appendFormHidden = function($form, name, val) {
        $("<input type='hidden' name='" + name + "' />")
            .val(val)
            .addClass('form-value')
            .appendTo($form);
    };

    var pageUser = function(user) {
        var $page = $('.user-properties').empty();

        var $form = $('<div>')
            .addClass('form')
            .appendTo($page);
        if (user === '') {  // new Client
            appendFormInput($form, 'NewUsername', 'text', 'New client');
            appendFormInput($form, 'NewPassword', 'password', 'New secret');

            $('<button>Create</button>')
                .appendTo($form)
                .click(function () {
                    postForm($form, {
                        url: '/manage/tokenusercreate',
                        success: function () {
                            pageSecurity();
                        }
                    });
                });
        }
        else {  // edit client
            appendFormHidden($form, 'Username', user);
            appendFormInput($form, 'NewPassword', 'password', 'New secret');

            $('<button>Change</button>')
                .appendTo($form)
                .click(function() {
                    postForm($form, {
                        url: '/manage/tokenuserchangepassword',
                        success: function() {
                            pageSecurity();
                        }
                    });
                });

            $('<button>Delete</button>')
                .css('background', 'red')
                .appendTo($form)
                .click(function () {
                    if (confirm('Are you sure? Delete Client?')) {
                        postForm($form, {
                            url: '/manage/tokenuserdelete',
                            success: function () {
                                pageSecurity();
                            }
                        });
                    }
                });
        }
    };

    var pageUrlToken = function (urlToken) {
        var $page = $('.urltoken-properties').empty();

        var $form = $('<div>')
            .addClass('form')
            .appendTo($page);
        if (urlToken === '') {  // new (Url) Token
            appendFormInput($form, 'NewTokenName', 'text', 'New Token Name');

            $('<button>Create</button>')
                .appendTo($form)
                .click(function () {
                    postForm($form, {
                        url: '/manage/urltokencreate',
                        success: function () {
                            pageSecurity();
                        }
                    });
                });
        }
        else  {  // edit token
            appendFormInput($form, 'UrlToken', 'text', 'Token', true, urlToken);

            $('<button>Recycle</button>')
                .appendTo($form)
                .click(function () {
                    if (confirm('Are you sure? Recycle Token?')) {
                        postForm($form, {
                            url: '/manage/urltokenrecycle',
                            success: function () {
                                pageSecurity('.urltoken.' + urlToken.split(UrlTokenSplitter)[0]);
                            }
                        });
                    }
                });

            $('<button>Delete</button>')
                .css('background','red')
                .appendTo($form)
                .click(function () {
                    if (confirm('Are you sure? Delete Token?')) {
                        postForm($form, {
                            url: '/manage/urltokendelete',
                            success: function () {
                                pageSecurity();
                            }
                        });
                    }
                });
        }
    };

    var pageSecurity = function(triggerClick) {
        var $body = $('.gview-manage-body')
            .empty()
            .addClass('loading');

        get({
            url: '/manage/tokenusers',
            success: function (result) {
                var $usersContainer = $("<div>")
                    .addClass('container')
                    .appendTo($body);

                var $users = $('<ul>')
                    .addClass('users')
                    .appendTo($usersContainer);

                $('<li>New Client...</li>')
                    .addClass('user new selected')
                    .appendTo($users)
                    .click(function() {
                        $(this)
                            .parent()
                            .children('.user')
                            .removeClass('selected');
                        $(this).addClass('selected');
                        pageUser('');
                    });
               
                $.each(result.users, function (i, user) {
                    if (user.indexOf(UrlTokenPrefix) === 0)
                        return;

                    $('<li>')
                        .addClass('user')
                        .attr('data-user', user)
                        .html(user)
                        .appendTo($users)
                        .click(function() {
                            $(this)
                                .parent()
                                .children('.user')
                                .removeClass('selected');
                            $(this).addClass('selected');
                            pageUser($(this).attr('data-user'));
                        });
                });

                $('<div>')
                    .addClass('user-properties')
                    .appendTo($usersContainer);

                // experimental
                var $urlTokensContainer = $("<div>")
                    .addClass('container')
                    .appendTo($body);

                var $urlTokens = $('<ul>')
                    .addClass('urltokens')
                    .appendTo($urlTokensContainer);
                $('<li>New (Url) Token</li>')
                    .addClass('urltoken new')
                    .appendTo($urlTokens)
                    .click(function () {
                        $(this)
                            .parent()
                            .children('.urltoken')
                            .removeClass('selected');
                        $(this).addClass('selected');
                        pageUrlToken('');
                    });

                $.each(result.users, function (i, urlToken) {
                    if (urlToken.indexOf(UrlTokenPrefix) !== 0)
                        return;

                    $('<li>')
                        .addClass('urltoken '+urlToken.split(UrlTokenSplitter)[0])
                        .attr('data-user', urlToken)
                        .html(urlToken.split(UrlTokenSplitter)[0])
                        .appendTo($urlTokens)
                        .click(function () {
                            $(this)
                                .parent()
                                .children('.user')
                                .removeClass('selected');
                            $(this).addClass('selected');
                            pageUrlToken($(this).attr('data-user'));
                        });
                });

                $('<div>')
                    .addClass('urltoken-properties')
                    .appendTo($urlTokensContainer);

                console.log('triggerCLick', triggerClick);
                $body.find(triggerClick || '.user.new').trigger('click');
            }
        });
    };

    var toUIString = function (type) {
        if (type) {
            switch (type.toLowerCase()) {
                case 'map':
                    return 'Export Map';
                case 'query':
                    return 'Query';
                case 'edit':
                    return 'Features';
                case 'publish':
                    return 'Publish Serivices';
            }
        }
        return type;
    }

    return {
        pageServices: pageServices,
        pageSecurity: pageSecurity,
        setRootUrl: setRootUrl
    };
})();
