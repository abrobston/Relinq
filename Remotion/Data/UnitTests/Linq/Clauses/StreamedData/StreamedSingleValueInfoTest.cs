// This file is part of the re-motion Core Framework (www.re-motion.org)
// Copyright (C) 2005-2009 rubicon informationstechnologie gmbh, www.rubicon.eu
// 
// The re-motion Core Framework is free software; you can redistribute it 
// and/or modify it under the terms of the GNU Lesser General Public License 
// as published by the Free Software Foundation; either version 2.1 of the 
// License, or (at your option) any later version.
// 
// re-motion is distributed in the hope that it will be useful, 
// but WITHOUT ANY WARRANTY; without even the implied warranty of 
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the 
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public License
// along with re-motion; if not, see http://www.gnu.org/licenses.
// 
using System;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using Remotion.Data.Linq;
using Remotion.Data.Linq.Clauses.StreamedData;
using Remotion.Data.UnitTests.Linq.TestDomain;
using Rhino.Mocks;

namespace Remotion.Data.UnitTests.Linq.Clauses.StreamedData
{
  [TestFixture]
  public class StreamedSingleValueInfoTest
  {
    private StreamedSingleValueInfo _streamedSingleValueInfoWithDefault;
    private StreamedSingleValueInfo _streamedSingleValueInfoNoDefault;

    [SetUp]
    public void SetUp ()
    {
      _streamedSingleValueInfoWithDefault = new StreamedSingleValueInfo (typeof (Student), true);
      _streamedSingleValueInfoNoDefault = new StreamedSingleValueInfo (typeof (Student), false);
    }

    [Test]
    public void ExecuteQueryModel_WithDefaultWhenEmpty ()
    {
      var queryModel = ExpressionHelper.CreateQueryModel_Student ();
      var student1 = new Student();
      
      var executorMock = MockRepository.GenerateMock<IQueryExecutor> ();
      executorMock.Expect (mock => mock.ExecuteSingle<Student> (queryModel, true)).Return (student1);
      
      var streamedData = _streamedSingleValueInfoWithDefault.ExecuteQueryModel (queryModel, executorMock);

      executorMock.VerifyAllExpectations ();

      Assert.That (streamedData, Is.InstanceOfType (typeof (StreamedValue)));
      Assert.That (streamedData.DataInfo, Is.SameAs (_streamedSingleValueInfoWithDefault));
      Assert.That (streamedData.Value, Is.EqualTo (student1));
    }

    [Test]
    public void ExecuteQueryModel_NoDefaultWhenEmpty ()
    {
      var queryModel = ExpressionHelper.CreateQueryModel_Student ();
      var student1 = new Student ();

      var executorMock = MockRepository.GenerateMock<IQueryExecutor> ();
      executorMock.Expect (mock => mock.ExecuteSingle<Student> (queryModel, false)).Return (student1);

      var streamedData = _streamedSingleValueInfoNoDefault.ExecuteQueryModel (queryModel, executorMock);

      executorMock.VerifyAllExpectations ();

      Assert.That (streamedData, Is.InstanceOfType (typeof (StreamedValue)));
      Assert.That (streamedData.DataInfo, Is.SameAs (_streamedSingleValueInfoNoDefault));
      Assert.That (streamedData.Value, Is.EqualTo (student1));
    }

    [Test]
    public void ExecuteQueryModel_WithValueType ()
    {
      var queryModel = ExpressionHelper.CreateQueryModel_Int ();

      var executorMock = MockRepository.GenerateMock<IQueryExecutor> ();
      executorMock.Expect (mock => mock.ExecuteSingle<int> (queryModel, true)).Return (5);

      var streamedSingleValueInfo = new StreamedSingleValueInfo (typeof (int), true);
      var streamedData = streamedSingleValueInfo.ExecuteQueryModel (queryModel, executorMock);

      executorMock.VerifyAllExpectations ();

      Assert.That (streamedData, Is.InstanceOfType (typeof (StreamedValue)));
      Assert.That (streamedData.DataInfo, Is.SameAs (streamedSingleValueInfo));
      Assert.That (streamedData.Value, Is.EqualTo (5));
    }

    [Test]
    [ExpectedException (typeof (InvalidOperationException), ExpectedMessage = "Test")]
    public void ExecuteQueryModel_WithException ()
    {
      var queryModel = ExpressionHelper.CreateQueryModel_Student ();

      var executorMock = MockRepository.GenerateMock<IQueryExecutor> ();
      executorMock.Expect (mock => mock.ExecuteSingle<Student> (queryModel, true)).Throw (new InvalidOperationException ("Test"));

      _streamedSingleValueInfoWithDefault.ExecuteQueryModel (queryModel, executorMock);
    }
  }
}
