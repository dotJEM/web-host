using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotJEM.Json.Index;
using DotJEM.Json.Storage;
using DotJEM.Json.Storage.Adapter;
using DotJEM.Web.Host.Diagnostics;
using DotJEM.Web.Host.Providers.Concurrency;
using DotJEM.Web.Host.Providers.Services;
using DotJEM.Web.Host.Util;
using Moq;
using Moq.AutoMock;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DotJEM.Web.Host.Test.Diagnostics
{
    [TestFixture]
    public class DiagnosticsLoggerTest
    {
        private AutoMocker mocker;
        private JObject error;

        [SetUp]
        public void SetUp()
        {
            mocker = new AutoMocker();

            error = JObject.FromObject(new
            {
                err = "This is an error, oh no !"
            });

            mocker.GetMock<IJsonConverter>().Setup(c => c.FromObject(Severity.Fatal)).Returns(Severity.Fatal.ToString());
            mocker.GetMock<IJsonConverter>().Setup(c => c.FromObject(Severity.Status)).Returns(Severity.Fatal.ToString());

            Mock<IStorageIndexManager> manager = mocker.GetMock<IStorageIndexManager>();
            Lazy<IStorageIndexManager> lazyStorageIndexManager = new Lazy<IStorageIndexManager>(() => manager.Object);
            mocker.Use(lazyStorageIndexManager);
            mocker.GetMock<IStorageArea>().Setup(x => x.Insert(It.IsAny<string>(), error)).Returns(error);
            Mock<IStorageContext> storageContext = mocker.GetMock<IStorageContext>();
            storageContext.Setup(x => x.Area(It.IsAny<string>())).Returns(mocker.GetMock<IStorageArea>().Object);
            Lazy<IStorageContext> lazyStorageContext = new Lazy<IStorageContext>(() => storageContext.Object);
            mocker.Use(lazyStorageContext);
        }

        [Test]
        public void DiagnosticsLogger_LogWithContentTypeAndSeverityAndEntity_AreaWasCalled()
        {            
            DiagnosticsLogger logger = mocker.CreateInstance<DiagnosticsLogger>();
            logger.Log(DiagnosticsLogger.ContentTypeIncident, Severity.Fatal, error);

            mocker.Verify<IStorageArea>(area => area.Insert(It.IsAny<string>(), It.IsAny<JObject>()), Times.Once());
        }

        [Test]
        public void DiagnosticsLogger_LogWithContentTypeAndSeverityAndEntity_IndexManagerWasCalled()
        {
            DiagnosticsLogger logger = mocker.CreateInstance<DiagnosticsLogger>();
            logger.Log(DiagnosticsLogger.ContentTypeIncident, Severity.Fatal, error);

            mocker.Verify<IStorageIndexManager>(manager => manager.QueueUpdate(It.IsAny<JObject>()), Times.Once());
        }

        [Test]
        public void DiagnosticsLogger_LogWithContentTypeAndSeverityAndEntity_SeverityIsPresentOnEntity()
        {
            DiagnosticsLogger logger = mocker.CreateInstance<DiagnosticsLogger>();
            dynamic result = logger.Log(DiagnosticsLogger.ContentTypeIncident, Severity.Fatal, error);

            Assert.That((string)result.severity, Is.EqualTo(Severity.Fatal.ToString()));
        }

        [Test]
        public void DiagnosticsLogger_LogWithContentTypeAndSeverityAndEntityAndMessage_MessageIsPresentOnEntity()
        {
            DiagnosticsLogger logger = mocker.CreateInstance<DiagnosticsLogger>();
            dynamic result = logger.Log(DiagnosticsLogger.ContentTypeIncident, Severity.Fatal, "This is a message", error);
            
            Assert.That((string)result.message, Is.EqualTo("This is a message"));
        }

        [Test]
        public void DiagnosticsLogger_LogIncident_AreaWasCalled()
        {
            DiagnosticsLogger logger = mocker.CreateInstance<DiagnosticsLogger>();
            logger.LogIncident(Severity.Fatal, error);

            mocker.Verify<IStorageArea>(area => area.Insert(It.IsAny<string>(), It.IsAny<JObject>()), Times.Once());
        }

        [Test]
        public void DiagnosticsLogger_LogIncident_IndexManagerWasCalled()
        {
            DiagnosticsLogger logger = mocker.CreateInstance<DiagnosticsLogger>();
            logger.LogIncident(Severity.Fatal, error);

            mocker.Verify<IStorageIndexManager>(manager => manager.QueueUpdate(It.IsAny<JObject>()), Times.Once());
        }

        [Test]
        public void DiagnosticsLogger_LogIncident_SeverityIsPresentOnEntity()
        {
            DiagnosticsLogger logger = mocker.CreateInstance<DiagnosticsLogger>();
            dynamic result = logger.LogIncident(Severity.Fatal, error);

            Assert.That((Severity)result.severity, Is.EqualTo(Severity.Fatal));
        }

        [Test]
        public void DiagnosticsLogger_LogIncident_MessageIsPresentOnEntity()
        {
            DiagnosticsLogger logger = mocker.CreateInstance<DiagnosticsLogger>();
            dynamic result = logger.LogIncident(Severity.Status, "I was called from LogIncident", error);

            Assert.That((string)result.message, Is.EqualTo("I was called from LogIncident"));
        }

        [Test]
        public void DiagnosticsLogger_LogWarning_AreaWasCalled()
        {
            DiagnosticsLogger logger = mocker.CreateInstance<DiagnosticsLogger>();
            logger.LogWarning(Severity.Fatal, error);

            mocker.Verify<IStorageArea>(area => area.Insert(It.IsAny<string>(), It.IsAny<JObject>()), Times.Once());
        }

        [Test]
        public void DiagnosticsLogger_LogWarning_IndexManagerWasCalled()
        {
            DiagnosticsLogger logger = mocker.CreateInstance<DiagnosticsLogger>();
            logger.LogWarning(Severity.Fatal, error);

            mocker.Verify<IStorageIndexManager>(manager => manager.QueueUpdate(It.IsAny<JObject>()), Times.Once());
        }

        [Test]
        public void DiagnosticsLogger_LogWarning_SeverityIsPresentOnEntity()
        {
            DiagnosticsLogger logger = mocker.CreateInstance<DiagnosticsLogger>();
            dynamic result = logger.LogWarning(Severity.Fatal, error);

            Assert.That((Severity)result.severity, Is.EqualTo(Severity.Fatal));
        }

        [Test]
        public void DiagnosticsLogger_LogWarning_AreaAndIndexManagerWasCalledAndSeverityIsPresentOnEntityAndMessageIsPresentOnEntity()
        {
            DiagnosticsLogger logger = mocker.CreateInstance<DiagnosticsLogger>();
            dynamic result = logger.LogWarning(Severity.Status, "I was called from LogWarning", error);

            Assert.That((string)result.message, Is.EqualTo("I was called from LogWarning"));
        }

        [Test]
        public void DiagnosticsLogger_LogFailure_AreaWasCalled()
        {
            DiagnosticsLogger logger = mocker.CreateInstance<DiagnosticsLogger>();
            logger.LogFailure(Severity.Fatal, error);

            mocker.Verify<IStorageArea>(area => area.Insert(It.IsAny<string>(), It.IsAny<JObject>()), Times.Once());
        }

        [Test]
        public void DiagnosticsLogger_LogFailure_IndexManagerWasCalled()
        {
            DiagnosticsLogger logger = mocker.CreateInstance<DiagnosticsLogger>();
            logger.LogFailure(Severity.Fatal, error);

            mocker.Verify<IStorageIndexManager>(manager => manager.QueueUpdate(It.IsAny<JObject>()), Times.Once());
        }

        [Test]
        public void DiagnosticsLogger_LogFailure_SeverityIsPresentOnEntity()
        {
            DiagnosticsLogger logger = mocker.CreateInstance<DiagnosticsLogger>();
            dynamic result = logger.LogFailure(Severity.Fatal, error);

            Assert.That((Severity)result.severity, Is.EqualTo(Severity.Fatal));
        }

        [Test]
        public void DiagnosticsLogger_LogFailure_AreaAndIndexManagerWasCalledAndSeverityIsPresentOnEntityAndMessageIsPresentOnEntity()
        {
            mocker.GetMock<IStorageArea>().Setup(x => x.Insert(It.IsAny<string>(), error)).Returns(error);
            mocker.GetMock<IStorageContext>().Setup(x => x.Area(It.IsAny<string>())).Returns(mocker.GetMock<IStorageArea>().Object);

            DiagnosticsLogger logger = mocker.CreateInstance<DiagnosticsLogger>();
            dynamic result = logger.LogFailure(Severity.Status, "I was called from LogFailure", error);

            Assert.That((string)result.message, Is.EqualTo("I was called from LogFailure"));        
        }

        [Test]
        public void DiagnosticsLogger_Log_ShouldContainMachineName()
        {
            mocker.GetMock<IStorageArea>().Setup(x => x.Insert(It.IsAny<string>(), error)).Returns(error);
            mocker.GetMock<IStorageContext>().Setup(x => x.Area(It.IsAny<string>())).Returns(mocker.GetMock<IStorageArea>().Object);

            DiagnosticsLogger logger = mocker.CreateInstance<DiagnosticsLogger>();
            dynamic result = logger.LogFailure(Severity.Status, "", error);

            Assert.That((string)result.host, Is.EqualTo(Environment.MachineName));      
        }
    }
}
