using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Core;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace EPiFakeMaker.Commerce
{
    public class FakeProduct<T> : Fake where T : EntryContentBase, new()
    {
        private readonly IList<IFake> _children;
        private static readonly Random Randomizer = new Random();

        private FakeProduct()
        {
            this._children = new List<IFake>();
        }

        public T Product
        {
            get
            {
                return this.Content as T;
            }
        }

        public override IContent Content { get; protected set; }

        public override IList<IFake> Children { get { return this._children; } }

        public static FakeProduct<T> Create(string productName)
        {
            FakeProduct<T> fake = new FakeProduct<T>()
            {
                Content = new T()
                {
                    Name = productName,
                    DisplayName = productName,
                    Code = productName
                }
            };

            fake.WithReferenceId(Randomizer.Next(10, 1000));

            fake.RepoGet = repo => repo.Get<T>(fake.Content.ContentLink);
            fake.LoaderGet = loader => loader.Get<T>(fake.Content.ContentLink);

            return fake;
        }

        public virtual FakeProduct<T> WithReferenceId(int referenceId)
        {
            this.Content.ContentLink = new ContentReference(referenceId);

            return this;
        }

        public virtual TOther To<TOther>() where TOther : class, IContent
        {
            return Content as TOther;
        }

        public virtual FakeProduct<T> ChildOf(IFake parent)
        {
            parent.Children.Add(this);

            this.Product.ParentLink = parent.Content.ContentLink;

            return this;
        }

        internal Expression<Func<IContentRepository, IContent>> RepoGet { get; private set; }
        internal Expression<Func<IContentLoader, IContent>> LoaderGet { get; private set; }

        internal override void HelpCreatingMockForCurrentType(IFakeMaker maker)
        {
            maker.CreateMockFor<T>(this);
            maker.CreateMockFor<T>(this, this.Children);
            maker.CreateMockFor(this, this.RepoGet);
            maker.CreateMockFor(this, this.LoaderGet);
        }
    }
}
