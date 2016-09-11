(function() {
    var app = angular.module('dotjem.demo', []);

    app.controller('ApplicationController', function ($content) {
        var self = this;
        var todos = $content('content', 'todo');

        refresh();

        self.createTodo = function () {
            todos.post(self.new).then(function(response) {
                self.new = {};
                refresh();
            });
        }

        self.applyFilter = refresh;

        this.message = "HELLO WORLD";

        function refresh() {
            var parts = [];
            if (self.filter) {
                if (self.filter.title) {
                    parts.push("title:" + self.filter.title + '*');
                }
                if (self.filter.type) {
                    parts.push("type:" + self.filter.type);
                }
                if (self.filter.type) {
                    parts.push("priority:[0 TO" + self.filter.priority + "]");
                }
            }

            var query = parts.join(" AND ");

            todos.search(query).then(function (response) {
                self.model = response;
            });
        }
    });

    Content.$inject = ['$http', '$queryInterpolate'];
    function Content(http, interpolate) {

        function interpolateUri(area, contentType, id) {
            return interpolate('api/{{ area }}/{{ contentType }}/{{ id }}', { area: area, contentType: contentType, id: id || '' });
        }

        function self(area, contentType) {
            return !contentType
                ? self.buildForArea(area)
                : self.buildForContentType(area, contentType);
        }

        self.search = function (query, params, config) {
            config = config || {};
            config.params = (angular.isDefined(params) ? angular.copy(params) : {});
            config.params.query = query;
            return http.get('api/search', config)
                .then(transform);
        }

        self.get = function (area, contentType, id, config) {
            var uri = interpolateUri(area, contentType, id);
            return http.get(uri, config)
                .then(transform);
        }

        self.post = function (area, contentType, entity, config) {
            var uri = interpolateUri(area, contentType);
            return http.post(uri, entity, config)
                .then(transform);
        }

        self.put = function (area, contentType, id, entity, config) {
            var uri = interpolateUri(area, contentType, id);
            return http.put(uri, entity, config)
                .then(transform);
        }

        self.delete = function (area, contentType, id, config) {
            var uri = interpolateUri(area, contentType, id);
            return http.delete(uri, config)
                .then(transform);
        }

        self.buildForContentType = function (area, contentType) {
            return {
                search: function (query, params, config) {
                    if (!query) {
                        return self.search("$area:" + area + " AND $contentType:" + contentType, params, config);
                    }

                    var parts = query.split('ORDER BY');
                    if (parts.length === 1) {
                        return self.search("$area:" + area + " AND $contentType:" + contentType + " AND ( " + query + " )", params, config);
                    }

                    query = parts[0].trim();
                    var order = parts[1].trim();
                    if (query.length === 0) {
                        return self.search("$area:" + area + " AND $contentType:" + contentType + " ORDER BY " + order, params, config);
                    }

                    return self.search("$area:" + area + " AND $contentType:" + contentType + " AND ( " + query + " ) ORDER BY " + order, params, config);
                },
                get: function (id, config) { return self.get(area, contentType, id, config); },
                post: function (entity, config) { return self.post(area, contentType, entity, config); },
                put: function (id, entity, config) { return self.put(area, contentType, id, entity, config); },
                delete: function (id, config) { return self.delete(area, contentType, id, config); }
            };
        };

        self.buildForArea = function (area) {
            return {
                search: function (query, params, config) {
                    if (!query) {
                        return self.search("$area:" + area, params, config);
                    }
                    var parts = query.split('ORDER BY');
                    if (parts.length === 1) {
                        return self.search("$area:" + area + " AND ( " + query + " )", params, config);
                    }

                    query = parts[0].trim();
                    var order = parts[1].trim();
                    if (query.length === 0) {
                        return self.search("$area:" + area + " ORDER BY " + order, params, config);
                    }

                    return self.search("$area:" + area + " AND ( " + query + " ) ORDER BY " + order, params, config);
                },
                get: function (contentType, id, config) { return self.get(area, contentType, id, config); },
                post: function (contentType, entity, config) { return self.post(area, contentType, entity, config); },
                put: function (contentType, id, entity, config) { return self.put(area, contentType, id, entity, config); },
                delete: function (contentType, id, config) { return self.delete(area, contentType, id, config); }
            }
        }

        return self;
    }

    app.factory('$content', Content);

    function transform(response) {
        return response.data;
    }

    app.factory('$queryInterpolate', [function () {

        function encodeUriSegment(val) {
            return encodeUriQuery(val, true).
                       replace(/%26/gi, '&').
                       replace(/%3D/gi, '=').
                       replace(/%2B/gi, '+');
        }

        function encodeUriQuery(val, pctEncodeSpaces) {
            return encodeURIComponent(val).
                       replace(/%40/gi, '@').
                       replace(/%3A/gi, ':').
                       replace(/%24/g, '$').
                       replace(/%2C/gi, ',').
                       replace(/%3B/gi, ';').
                       replace(/%20/g, (pctEncodeSpaces ? '%20' : '+'));
        }

        //TODO: use $interpolate... but replacing it directly fails atm.
        var regex = /\{\{\s*(\w+)\s*\}\}/g;
        function interpolate(url, values) {
            var replaced = url.replace(regex, function (key) {
                var name = key.substring(2, key.length - 2).trim();
                return values.hasOwnProperty(name) ? encodeUriSegment(values[name]) : key;
            });
            return replaced;
        }
        return interpolate;
    }]);
})();