// This file is part of the re-motion Core Framework (www.re-motion.org)
// Copyright (C) 2005-2009 rubicon informationstechnologie gmbh, www.rubicon.eu
// 
// The re-motion Core Framework is free software; you can redistribute it 
// and/or modify it under the terms of the GNU Lesser General Public License 
// version 3.0 as published by the Free Software Foundation.
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
using System.Collections.Generic;
using System.Linq.Expressions;
using Remotion.Data.Linq.Clauses;
using Remotion.Data.Linq.Clauses.Expressions;
using Remotion.Data.Linq.Parsing.ExpressionTreeVisitors;
using Remotion.Utilities;

namespace Remotion.Data.Linq.Parsing.Structure.IntermediateModel
{
  /// <summary>
  /// Represents a <see cref="ConstantExpression"/> which acts as a query source.
  /// It is generated by <see cref="ExpressionTreeParser"/> when an <see cref="Expression"/> tree is parsed.
  /// This node usually marks the end (i.e. the first node) of an <see cref="IExpressionNode"/> chain that represents a query.
  /// </summary>
  public class ConstantExpressionNode : IQuerySourceExpressionNode
  {
    public ConstantExpressionNode (string associatedIdentifier, Type querySourceType, object value)
    {
      ArgumentUtility.CheckNotNull ("querySourceType", querySourceType);
      ArgumentUtility.CheckNotNullOrEmpty ("associatedIdentifier", associatedIdentifier);

      QuerySourceType = querySourceType;
      QuerySourceElementType = GetQuerySourceElementType (querySourceType);
      Value = value;
      AssociatedIdentifier = associatedIdentifier;
    }

    private Type GetQuerySourceElementType (Type enumerableType)
    {
      try
      {
        // To get the element type streamed out by this node, we try to see what kind of IEnumerable<T> is implemented by the given type.
        // T is the element type we want to find out.
        return ReflectionUtility.GetAscribedGenericArguments (enumerableType, typeof (IEnumerable<>))[0];
      }
      catch (ArgumentTypeException)
      {
        throw new ArgumentTypeException ("expression", typeof (IEnumerable<>), enumerableType);
      }
    }

    public Type QuerySourceElementType { get; private set; }
    public Type QuerySourceType { get; set; }
    public object Value { get; private set; }
    public string AssociatedIdentifier { get; set; }

    public IExpressionNode Source
    {
      get { return null; }
    }

    public Expression Resolve (ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext)
    {
      ArgumentUtility.CheckNotNull ("inputParameter", inputParameter);
      ArgumentUtility.CheckNotNull ("expressionToBeResolved", expressionToBeResolved);

      // query sources resolve into references that point back to the respective clauses
      FromClauseBase clause = GetClauseForResolve (clauseGenerationContext.ClauseMapping);
      var reference = new QuerySourceReferenceExpression (clause);
      return ReplacingVisitor.Replace (inputParameter, reference, expressionToBeResolved);
    }

    private FromClauseBase GetClauseForResolve (QuerySourceClauseMapping querySourceClauseMapping)
    {
      try
      {
        return querySourceClauseMapping.GetClause (this);
      }
      catch (KeyNotFoundException ex)
      {
        var message = string.Format (
            "Cannot resolve with a {0} for which no clause was created. Be sure to call CreateClause before calling Resolve, and pass in the same " 
            + "QuerySourceClauseMapping to both methods.", 
            GetType().Name);
        throw new InvalidOperationException (message, ex);
      }
    }

    public void Apply (QueryModel queryModel, ClauseGenerationContext clauseGenerationContext)
    {
      ArgumentUtility.CheckNotNull ("queryModel", queryModel);

      var fromClause = new MainFromClause (
          AssociatedIdentifier,
          QuerySourceElementType,
          Expression.Constant (Value, QuerySourceType));

      clauseGenerationContext.ClauseMapping.AddMapping (this, fromClause);
      queryModel.MainFromClause = fromClause;
    }

    public IClause CreateClause (IClause previousClause, ClauseGenerationContext clauseGenerationContext)
    {
      if (previousClause != null)
      {
        throw new InvalidOperationException (
            "A ConstantExpressionNode cannot create a clause with a previous clause because it represents the end "
            + "of a query call chain. Set previousClause to null.");
      }

      var fromClause = new MainFromClause (
          AssociatedIdentifier,
          QuerySourceElementType,
          Expression.Constant (Value, QuerySourceType));

      clauseGenerationContext.ClauseMapping.AddMapping (this, fromClause);
      return fromClause;
    }

    public SelectClause CreateSelectClause (IClause previousClause, ClauseGenerationContext clauseGenerationContext)
    {
      var parameterExpression = CreateParameterForOutput();
      var selector = Resolve (parameterExpression, parameterExpression, clauseGenerationContext);
      return new SelectClause (selector);
    }

    public ParameterExpression CreateParameterForOutput ()
    {
      return Expression.Parameter (QuerySourceElementType, AssociatedIdentifier);
    }
  }
}