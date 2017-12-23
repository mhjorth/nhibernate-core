﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by AsyncGenerator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------


using NUnit.Framework;

namespace NHibernate.Test.NHSpecificTest.NH1859
{
	using System.Threading.Tasks;
	[TestFixture]
	public class SampleTestAsync : BugTestCase
	{
		protected override void OnSetUp()
		{
			base.OnSetUp();
			using (ISession session = OpenSession())
			{
				session.Save(new DomainClass {Id = 1});
				session.Flush();
			}
		}

		protected override void OnTearDown()
		{
			base.OnTearDown();
			using (ISession session = OpenSession())
			{
				session.Delete("from DomainClass");
				session.Flush();
			}
		}

		[Test]
		public async Task NativeQueryWithTwoCommentsAsync()
		{
			using (ISession session = OpenSession())
			{
				IQuery qry = session.CreateSQLQuery("select /* first comment */ o.* /* second comment*/ from domainclass o")
					.AddEntity("o", typeof (DomainClass));
				var res = await (qry.ListAsync<DomainClass>());
				Assert.AreEqual(res[0].Id, 1);
			}
		}
	}
}