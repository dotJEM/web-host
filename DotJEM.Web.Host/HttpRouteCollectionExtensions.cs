using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Routing;

namespace DotJEM.Web.Host
{
    public static class HttpRouteCollectionExtensions
    {
        /// <summary>
        /// Maps the specified route template.
        /// </summary>
        /// <param name="routes">A collection of config for the application.</param>
        /// <param name="name">The name of the route to map.</param>
        /// <param name="routeTemplate">The route template for the route.</param>
        /// <returns>A reference to the mapped route.</returns>
        public static IHttpRoute MapHttpRoute<TController>(this HttpRouteCollection routes, string name, string routeTemplate)
        {
            return MapHttpRoute<TController>(routes, name, routeTemplate, null, null, null);
        }

        /// <summary>
        /// Maps the specified route template and sets default constraints.
        /// </summary>
        /// <param name="routes">A collection of config for the application.</param>
        /// <param name="name">The name of the route to map.</param>
        /// <param name="routeTemplate">The route template for the route.</param>
        /// <param name="defaults">An object that contains default route values.</param>
        /// <returns>A reference to the mapped route.</returns>
        public static IHttpRoute MapHttpRoute<TController>(this HttpRouteCollection routes, string name, string routeTemplate, object defaults)
        {
            return MapHttpRoute<TController>(routes, name, routeTemplate, defaults, null, null);
        }

        /// <summary>
        /// Maps the specified route template and sets default route values and constraints.
        /// </summary>
        /// <param name="routes">A collection of config for the application.</param>
        /// <param name="name">The name of the route to map.</param>
        /// <param name="routeTemplate">The route template for the route.</param>
        /// <param name="defaults">An object that contains default route values.</param>
        /// <param name="constraints">A set of expressions that specify values for <paramref name="routeTemplate"/>.</param>
        /// <returns>A reference to the mapped route.</returns>
        public static IHttpRoute MapHttpRoute<TController>(this HttpRouteCollection routes, string name, string routeTemplate, object defaults, object constraints)
        {
            return MapHttpRoute<TController>(routes, name, routeTemplate, defaults, constraints, null);
        }

        /// <summary>
        /// Maps the specified route template and sets default route values, constraints, and end-point message handler.
        /// </summary>
        /// <param name="routes">A collection of config for the application.</param>
        /// <param name="name">The name of the route to map.</param>
        /// <param name="routeTemplate">The route template for the route.</param>
        /// <param name="defaults">An object that contains default route values.</param>
        /// <param name="constraints">A set of expressions that specify values for <paramref name="routeTemplate"/>.</param>
        /// <param name="handler">The handler to which the request will be dispatched.</param>
        /// <returns>A reference to the mapped route.</returns>
        public static IHttpRoute MapHttpRoute<TController>(this HttpRouteCollection routes, string name, string routeTemplate, object defaults, object constraints, HttpMessageHandler handler)
        {
            if (routes == null)
            {
                throw new ArgumentNullException("routes");
            }

            HttpRouteValueDictionary defaultsDictionary = new HttpRouteValueDictionary(defaults);
            HttpRouteValueDictionary constraintsDictionary = new HttpRouteValueDictionary(constraints);
            IHttpRoute route = CreateRoute<TController>(routeTemplate, defaultsDictionary, constraintsDictionary, null, handler);

            routes.Add(name, route);
            return route;
        }

        private static IHttpRoute CreateRoute<TController>(string routeTemplate, IDictionary<string, object> defaults, IDictionary<string, object> constraints, IDictionary<string, object> dataTokens, HttpMessageHandler handler)
        {
            HttpRouteValueDictionary routeDefaults = new HttpRouteValueDictionary(defaults);
            HttpRouteValueDictionary routeConstraints = new HttpRouteValueDictionary(constraints);
            HttpRouteValueDictionary routeDataTokens = new HttpRouteValueDictionary(dataTokens);
            return new WebRoute<TController>(routeTemplate, routeDefaults, routeConstraints, routeDataTokens, handler);
        }
    }
}