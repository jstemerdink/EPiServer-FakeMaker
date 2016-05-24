using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using EPiServer;
using EPiServer.Core;
using EPiServer.Web;
using Moq;

namespace EPiFakeMaker
{
    public class FakePage<T> : Fake where T : PageData, new()
    {
        private readonly IList<IFake> _children;
        private Mock<SiteDefinition> _siteDefinitonMock;
        private static readonly Random Randomizer = new Random();

        private FakePage()
        {
            _children = new List<IFake>();
        }

        /// <summary>
        /// Convenience feature that convert the Content property to PageData
        /// </summary>
        public virtual T Page
        {
            get
            {
                return this.Content as T;
            }
        }

        public override IContent Content { get; protected set; }

        public override IList<IFake> Children { get { return _children; } }

        public static FakePage<T> Create(string pageName)
        {
            FakePage<T> fake = new FakePage<T> { Content = new T() };

            fake.Content.Property["PageName"] = new PropertyString(pageName);

            fake.WithReferenceId(Randomizer.Next(10, 1000));

            fake.VisibleInMenu();

            fake.RepoGet = repo => repo.Get<T>(fake.Content.ContentLink);
            fake.LoaderGet = loader => loader.Get<T>(fake.Content.ContentLink);

            return fake;
        }

        public virtual FakePage<T> ChildOf(IFake parent)
        {
            parent.Children.Add(this);

            Content.Property["PageParentLink"] = new PropertyPageReference(parent.Content.ContentLink);

            return this;
        }

        public virtual FakePage<T> PublishedOn(DateTime publishDate)
        {
            PublishedOn(publishDate, null);

            return this;
        }

        public virtual FakePage<T> PublishedOn(DateTime publishDate, DateTime? stopPublishDate)
        {
            Content.Property["PageStartPublish"] = new PropertyDate(publishDate);

            WorkStatus(VersionStatus.Published);

            StopPublishOn(stopPublishDate.HasValue ? stopPublishDate.Value : publishDate.AddYears(1));

            return this;
        }

        public virtual FakePage<T> VisibleInMenu()
        {
            return SetMenuVisibility(true);
        }

        public virtual FakePage<T> HiddenFromMenu()
        {
            return SetMenuVisibility(false);
        }

        public virtual FakePage<T> SetMenuVisibility(bool isVisible)
        {
            Content.Property["PageVisibleInMenu"] = new PropertyBoolean(isVisible);

            return this;
        }

        public virtual FakePage<T> WithReferenceId(int referenceId)
        {
            Content.Property["PageLink"] = new PropertyPageReference(new PageReference(referenceId));

            return this;
        }

        public virtual FakePage<T> WithLanguageBranch(string languageBranch)
        {
            Content.Property["PageLanguageBranch"] = new PropertyString(languageBranch);

            return this;
        }

        public virtual FakePage<T> WithProperty(string propertyName, PropertyData propertyData)
        {
            Content.Property[propertyName] = propertyData;

            return this;
        }

        public virtual FakePage<T> WithContentTypeId(int contentTypeId)
        {
            Content.Property["PageTypeID"] = new PropertyNumber(contentTypeId);

            return this;
        }

        public virtual FakePage<T> WithChildren(IEnumerable<FakePage<T>> children)
        {
            children.ToList().ForEach(c => c.ChildOf(this));

            return this;
        }

        public virtual FakePage<T> StopPublishOn(DateTime stopPublishDate)
        {
            Content.Property["PageStopPublish"] = new PropertyDate(stopPublishDate);

            return this;
        }

        public virtual FakePage<T> WorkStatus(VersionStatus status)
        {
            Content.Property["PageWorkStatus"] = new PropertyNumber((int)status);

            return this;
        }

        public virtual FakePage<T> AsStartPage()
        {
            if (_siteDefinitonMock == null)
            {
                _siteDefinitonMock = SetupSiteDefinition();
            }

            _siteDefinitonMock.SetupGet(def => def.StartPage).Returns(Content.ContentLink);

            return this;
        }

        private static Mock<SiteDefinition> SetupSiteDefinition()
        {
            Mock<SiteDefinition> mock = new Mock<SiteDefinition>();

            mock.SetupGet(def => def.Name).Returns("FakeMakerSiteDefinition");

            SiteDefinition.Current = mock.Object;

            return mock;
        }

        public virtual TOther To<TOther>() where TOther : class, IContent
        {
            return Content as TOther;
        }

        internal Expression<Func<IContentRepository, IContent>> RepoGet { get; private set; }
        internal Expression<Func<IContentLoader, IContent>> LoaderGet { get; private set; }

        internal override void HelpCreatingMockForCurrentType(IFakeMaker maker)
        {
            maker.CreateMockFor<T>(this);
            maker.CreateMockFor<T>(this, Children);
            maker.CreateMockFor(this, RepoGet);
            maker.CreateMockFor(this, LoaderGet);
        }
    }
}
