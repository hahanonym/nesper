///////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2006-2019 Esper Team. All rights reserved.                           /
// http://esper.codehaus.org                                                          /
// ---------------------------------------------------------------------------------- /
// The software in this package is published under the terms of the GPL license       /
// a copy of which has been included with this distribution in the license.txt file.  /
///////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using com.espertech.esper.collection;
using com.espertech.esper.common.client;
using com.espertech.esper.common.client.util;
using com.espertech.esper.common.@internal.bytecodemodel.@base;
using com.espertech.esper.common.@internal.bytecodemodel.model.expression;
using com.espertech.esper.common.@internal.compile.stage3;
using com.espertech.esper.common.@internal.context.aifactory.core;
using com.espertech.esper.common.@internal.context.module;
using com.espertech.esper.common.@internal.epl.expression.core;
using com.espertech.esper.common.@internal.epl.streamtype;
using com.espertech.esper.common.@internal.@event.core;
using com.espertech.esper.common.@internal.util;
using com.espertech.esper.compat;
using com.espertech.esper.compat.collections;
using static com.espertech.esper.common.@internal.bytecodemodel.model.expression.CodegenExpressionBuilder;
using static com.espertech.esper.common.@internal.epl.expression.codegen.ExprForgeCodegenNames;
using static com.espertech.esper.common.@internal.filterspec.FilterSpecParam;

namespace com.espertech.esper.common.@internal.filterspec
{
    /// <summary>
    ///     This class represents an arbitrary expression node returning a boolean value as a filter parameter in an
    ///     <seealso cref="FilterSpecActivatable" /> filter specification.
    /// </summary>
    public sealed class FilterSpecParamExprNodeForge : FilterSpecParamForge
    {
        private readonly IDictionary<string, Pair<EventType, string>> _arrayEventTypes;
        private readonly StatementCompileTimeServices _compileTimeServices;
        private readonly bool _hasFilterStreamSubquery;
        private readonly bool _hasTableAccess;
        private readonly bool _hasVariable;
        private readonly StreamTypeService _streamTypeService;
        private readonly IDictionary<string, Pair<EventType, string>> _taggedEventTypes;

        public FilterSpecParamExprNodeForge(
            ExprFilterSpecLookupableForge lookupable,
            FilterOperator filterOperator,
            ExprNode exprNode,
            IDictionary<string, Pair<EventType, string>> taggedEventTypes,
            IDictionary<string, Pair<EventType, string>> arrayEventTypes,
            StreamTypeService streamTypeService,
            bool hasSubquery,
            bool hasTableAccess,
            bool hasVariable,
            StatementCompileTimeServices compileTimeServices)
            : base(lookupable, filterOperator)
        {
            if (filterOperator != FilterOperator.BOOLEAN_EXPRESSION) {
                throw new ArgumentException("Invalid filter operator for filter expression node");
            }

            ExprNode = exprNode;
            _taggedEventTypes = taggedEventTypes;
            _arrayEventTypes = arrayEventTypes;
            _streamTypeService = streamTypeService;
            _hasFilterStreamSubquery = hasSubquery;
            _hasTableAccess = hasTableAccess;
            _hasVariable = hasVariable;
            _compileTimeServices = compileTimeServices;
        }

        /// <summary>
        ///     Returns the expression node of the boolean expression this filter parameter represents.
        /// </summary>
        /// <returns>expression node</returns>
        public ExprNode ExprNode { get; }

        public int FilterBoolExprId { get; set; } = -1;

        /// <summary>
        ///     Returns the map of tag/stream names to event types that the filter expressions map use (for patterns)
        /// </summary>
        /// <value>map</value>
        public IDictionary<string, Pair<EventType, string>> TaggedEventTypes {
            get { return _taggedEventTypes; }
        }

        public override string ToString()
        {
            return base.ToString() + "  exprNode=" + ExprNode;
        }

        public override bool Equals(object obj)
        {
            if (this == obj) {
                return true;
            }

            if (!(obj is FilterSpecParamExprNodeForge)) {
                return false;
            }

            var other = (FilterSpecParamExprNodeForge) obj;
            if (!base.Equals(other)) {
                return false;
            }

            if (ExprNode != other.ExprNode) {
                return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            var result = base.GetHashCode();
            result = 31 * result + ExprNode.GetHashCode();
            return result;
        }

        public override CodegenMethod MakeCodegen(
            CodegenClassScope classScope,
            CodegenMethodScope parent,
            SAIFFInitializeSymbolWEventType symbols)
        {
            if (FilterBoolExprId == -1) {
                throw new IllegalStateException("Unassigned filter boolean expression path num");
            }

            var method = parent.MakeChild(typeof(FilterSpecParamExprNode), GetType(), classScope);
            method.Block
                .DeclareVar(typeof(ExprFilterSpecLookupable), "lookupable", LocalMethod(lookupable.MakeCodegen(method, symbols, classScope)))
                .DeclareVar(typeof(FilterOperator), "op", EnumValue(typeof(FilterOperator), filterOperator.GetName()));

            // getFilterValue-FilterSpecParamExprNode code
            var param = NewAnonymousClass(
                method.Block, typeof(FilterSpecParamExprNode),
                CompatExtensions.AsList<CodegenExpression>(Ref("lookupable"), Ref("op")));
            var getFilterValue = CodegenMethod.MakeParentNode(typeof(object), GetType(), classScope).AddParam(GET_FILTER_VALUE_FP);
            param.AddMethod("getFilterValue", getFilterValue);

            if (_taggedEventTypes != null && !_taggedEventTypes.IsEmpty() || _arrayEventTypes != null && !_arrayEventTypes.IsEmpty()) {
                var size = _taggedEventTypes != null ? _taggedEventTypes.Count : 0;
                size += _arrayEventTypes != null ? _arrayEventTypes.Count : 0;
                getFilterValue.Block.DeclareVar(typeof(EventBean[]), "events", NewArrayByLength(typeof(EventBean), Constant(size + 1)));

                var count = 1;
                if (_taggedEventTypes != null) {
                    foreach (var tag in _taggedEventTypes.Keys) {
                        getFilterValue.Block.AssignArrayElement(
                            "events", Constant(count), ExprDotMethod(REF_MATCHEDEVENTMAP, "getMatchingEventByTag", Constant(tag)));
                        count++;
                    }
                }

                if (_arrayEventTypes != null) {
                    foreach (var entry in _arrayEventTypes) {
                        var compositeEventType = entry.Value.First;
                        var compositeEventTypeMember = classScope.AddFieldUnshared(
                            true, typeof(EventType), EventTypeUtility.ResolveTypeCodegen(compositeEventType, EPStatementInitServicesConstants.REF));
                        var factory = classScope.AddOrGetFieldSharable(EventBeanTypedEventFactoryCodegenField.INSTANCE);
                        var matchingAsMap = ExprDotMethod(REF_MATCHEDEVENTMAP, "getMatchingEventsAsMap");
                        var mapBean = ExprDotMethod(factory, "adapterForTypedMap", matchingAsMap, compositeEventTypeMember);
                        getFilterValue.Block.AssignArrayElement("events", Constant(count), mapBean);
                        count++;
                    }
                }
            }
            else {
                getFilterValue.Block.DeclareVar(typeof(EventBean[]), "events", ConstantNull());
            }

            getFilterValue.Block
                .MethodReturn(
                    ExprDotMethod(
                        Ref("filterBooleanExpressionFactory"), "make",
                        Ref("this"), // FilterSpecParamExprNode filterSpecParamExprNode
                        Ref("events"), // EventBean[] events
                        REF_EXPREVALCONTEXT, // ExprEvaluatorContext exprEvaluatorContext
                        ExprDotMethod(REF_EXPREVALCONTEXT, "getAgentInstanceId"), // int agentInstanceId
                        REF_STMTCTXFILTEREVALENV));

            // expression evaluator
            var evaluator = ExprNodeUtilityCodegen.CodegenEvaluatorNoCoerce(ExprNode.Forge, method, GetType(), classScope);

            // setter calls
            method.Block
                .DeclareVar(typeof(FilterSpecParamExprNode), "node", param)
                .SetProperty(Ref("node"), "ExprText",
                    Constant(StringValue.StringDelimitedTo60Char(ExprNodeUtilityPrint.ToExpressionStringMinPrecedenceSafe(ExprNode))))
                .SetProperty(Ref("node"), "ExprNode", evaluator)
                .SetProperty(Ref("node"), "HasVariable", Constant(_hasVariable))
                .SetProperty(Ref("node"), "HasFilterStreamSubquery", Constant(_hasFilterStreamSubquery))
                .SetProperty(Ref("node"), "FilterBoolExprId", Constant(FilterBoolExprId))
                .SetProperty(Ref("node"), "HasTableAccess", Constant(_hasTableAccess))
                .SetProperty(Ref("node"), "FilterBooleanExpressionFactory",
                    ExprDotMethodChain(symbols.GetAddInitSvc(method)).Add(EPStatementInitServicesConstants.GETFILTERBOOLEANEXPRESSIONFACTORY))
                .SetProperty(Ref("node"), "UseLargeThreadingProfile",
                    Constant(_compileTimeServices.Configuration.Common.Execution.ThreadingProfile == ThreadingProfile.LARGE));

            if (_taggedEventTypes != null && !_taggedEventTypes.IsEmpty() || _arrayEventTypes != null && !_arrayEventTypes.IsEmpty()) {
                var size = _taggedEventTypes != null ? _taggedEventTypes.Count : 0;
                size += _arrayEventTypes != null ? _arrayEventTypes.Count : 0;
                method.Block.DeclareVar(typeof(EventType[]), "providedTypes", NewArrayByLength(typeof(EventType), Constant(size + 1)));
                for (var i = 1; i < _streamTypeService.StreamNames.Length; i++) {
                    var tag = _streamTypeService.StreamNames[i];
                    var eventType = FindMayNull(tag, _taggedEventTypes);
                    if (eventType == null) {
                        eventType = FindMayNull(tag, _arrayEventTypes);
                    }

                    if (eventType == null) {
                        throw new IllegalStateException("Failed to find event type for tag '" + tag + "'");
                    }

                    method.Block.AssignArrayElement(
                        "providedTypes", Constant(i), EventTypeUtility.ResolveTypeCodegen(eventType, EPStatementInitServicesConstants.REF));
                    // note: we leave index zero at null as that is the current event itself
                }

                method.Block.SetProperty(Ref("node"), "EventTypesProvidedBy", Ref("providedTypes"));
            }

            // register boolean expression so it can be found
            method.Block.Expression(
                ExprDotMethodChain(symbols.GetAddInitSvc(method)).Add(EPStatementInitServicesConstants.GETFILTERSHAREDBOOLEXPRREGISTERY)
                    .Add("registerBoolExpr", Ref("node")));

            method.Block.MethodReturn(Ref("node"));
            return method;
        }

        private EventType FindMayNull(
            string tag,
            IDictionary<string, Pair<EventType, string>> tags)
        {
            if (tags == null || !tags.ContainsKey(tag)) {
                return null;
            }

            return tags.Get(tag).First;
        }
    }
} // end of namespace