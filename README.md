[![Build status](https://ci.appveyor.com/api/projects/status/lj72kp8ldr5wuu2t?svg=true)](https://ci.appveyor.com/project/jeme/web-host)
[![FOSSA Status](https://app.fossa.com/api/projects/git%2Bgithub.com%2FdotJEM%2Fweb-host.svg?type=shield)](https://app.fossa.com/projects/git%2Bgithub.com%2FdotJEM%2Fweb-host?ref=badge_shield)

DEPRECATED
========

This package is only maintained for legacy projects, it won't follow SEMVER versioning anymore and it won't receive any features with a broader perspective than for those projects using it.
The plan is to migrate many of the concepts to .NET 8+ but in a completely different style and with a more "pick and choose" style aproach.

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


## License
[![FOSSA Status](https://app.fossa.com/api/projects/git%2Bgithub.com%2FdotJEM%2Fweb-host.svg?type=large)](https://app.fossa.com/projects/git%2Bgithub.com%2FdotJEM%2Fweb-host?ref=badge_large)
