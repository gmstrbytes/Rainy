// Declare app level module which depends on filters, and services
var app = angular.module('clientApp', [
    'clientApp.filters',
    'clientApp.services',
    'clientApp.directives',

    // anguar-strap.js
    '$strap.directives'
])
.config(['$routeProvider',
    function($routeProvider) {
        // login page
        $routeProvider.when('/login', {
            templateUrl: 'login.html',
            controller: 'LoginCtrl'
        });

        $routeProvider.when('/notes/:guid', {
            templateUrl: 'notes.html',
            controller: 'NoteCtrl'
        });
        $routeProvider.when('/logout', {
            template: '<div ng-controller="LogoutCtrl"></div>',
            controller: 'LogoutCtrl'
        });

        $routeProvider.otherwise({
            redirectTo: '/login'
        });
    }
])
// disable the X-Requested-With header
.config(['$httpProvider', function($httpProvider) {
        delete $httpProvider.defaults.headers.common['X-Requested-With'];
    }
])
.config(['$locationProvider',
    function($locationProvider) {
        $locationProvider.html5Mode(false);
    }
]);

// register the interceptor as a service
app.factory('loginInterceptor', function($q, $location) {
    return function(promise) {
        return promise.then(function(response) {
            // do something on success
            return response;
        }, function(response) {
            // do something on error
            if (response.status === 401) {
                if (window.localStorage)
                    window.localStorage.removeItem('accessToken');
                $location.path('/login');
            }
            return $q.reject(response);
        });
    };
});
app.config(['$httpProvider', function($httpProvider) {
        $httpProvider.responseInterceptors.push('loginInterceptor');
    }
]);


// FILTERS
angular.module('clientApp.filters', [])
    .filter('interpolate', ['version', function(version) {
        return function(text) {
            return String(text).replace(/\%VERSION\%/mg, version);
        };
    }]);

// SERVICES
angular.module('clientApp.services', [])
    .value('version', '0.1');


// DIRECTIVES 
angular.module('clientApp.directives', [])
    .directive('appVersion', ['version',
        function(version) {
            return function(scope, elm, attrs) {
                elm.text(version);
            };
        }
    ])
;

if (typeof String.prototype.startsWith !== 'function') {
    String.prototype.startsWith = function(str) {
        return this.slice(0, str.length) === str;
    };
}
