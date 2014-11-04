web-host
========

Simple oppinionated abstraction on top of the Web API for SPA's that provides a data in SQL Server backend, lucene indexing and castle windsor DI out of the box.

Simply implement a WebHost, override the needed configuration hooks and start.

```C#
public class MyHost : WebHost {
  protected override void Configure(IWindsorContainer container) { /*...*/ }
  protected override void Configure(IStorageContext storage) { /*...*/ }
  protected override void Configure(IStorageIndex index) { /*...*/ }
  
  protected override void Configure(IRouter router) {
    router.Route('ignorePattern').Through()
          .Route('apiPattern').To<MyApiController>()
          .Route('otherApiPattern').To<MyOtherApiController>(config => config.Set.Defaults(new {}));
          .Otherwise().To<DefaultController>(); //Normaly an PageController returning the SPA application.
  }
  
  protected override void Initialize(IStorageIndex index) {
    //When using RAM based indexes, load entries from the Storage or restore it from a Cache.
  }
}

public class WebApiApplication : HttpApplication {
  protected void Application_Start()
  {
    AreaRegistration.RegisterAllAreas();
    GlobalFilters.Filters.Add(new HandleErrorAttribute());
    new MyHost().Start();
  }
}
```
