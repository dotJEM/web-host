using System;
using System.Collections.Generic;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using DotJEM.Json.Index;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.Services;

public interface ITermService
{
    JObject Get(string contentType, string field);
}

public class TermService : ITermService
{
    private readonly IStorageIndex index;

    public TermService(IStorageIndex index)
    {
        this.index = index;
    }

    public JObject Get(string contentType, string field)
    {
        if (field == null) 
            throw new ArgumentNullException("field");

        if (contentType == null)
            throw new ArgumentNullException("contentType");

        if(string.IsNullOrWhiteSpace(field))
            throw new ArgumentException("field was empty or only had whitespaces.","field");

        if (string.IsNullOrWhiteSpace(contentType))
            throw new ArgumentException("contentType was empty or only had whitespaces.", "field");

        dynamic value = new JObject();
        value.terms = index.Terms(field);
        return value;
    }
}