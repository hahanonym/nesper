///////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2006-2019 Esper Team. All rights reserved.                           /
// http://esper.codehaus.org                                                          /
// ---------------------------------------------------------------------------------- /
// The software in this package is published under the terms of the GPL license       /
// a copy of which has been included with this distribution in the license.txt file.  /
///////////////////////////////////////////////////////////////////////////////////////

using System.Collections.Generic;
using com.espertech.esper.common.client;
using com.espertech.esper.common.@internal.bytecodemodel.@base;
using com.espertech.esper.common.@internal.bytecodemodel.model.expression;
using com.espertech.esper.common.@internal.@event.core;
using com.espertech.esper.compat.collections;
using static com.espertech.esper.common.@internal.bytecodemodel.model.expression.CodegenExpressionBuilder;

namespace com.espertech.esper.common.@internal.@event.map
{
    public class MapNestedEntryPropertyGetterPropertyProvidedDynamic : MapNestedEntryPropertyGetterBase
    {
        private readonly EventPropertyGetter nestedGetter;

        public MapNestedEntryPropertyGetterPropertyProvidedDynamic(
            string propertyMap,
            EventType fragmentType,
            EventBeanTypedEventFactory eventBeanTypedEventFactory,
            EventPropertyGetter nestedGetter)
            : base(propertyMap, fragmentType, eventBeanTypedEventFactory)
        {
            this.nestedGetter = nestedGetter;
        }

        public override bool IsExistsProperty(EventBean eventBean)
        {
            return IsExistsProperty(BaseNestableEventUtil.CheckedCastUnderlyingMap(eventBean));
        }

        public override object HandleNestedValue(object value)
        {
            if (!(value is IDictionary<string, object>)) {
                return null;
            }

            if (nestedGetter is MapEventPropertyGetter) {
                return ((MapEventPropertyGetter) nestedGetter).GetMap((IDictionary<string, object>) value);
            }

            return null;
        }

        private CodegenMethod HandleNestedValueCodegen(
            CodegenMethodScope codegenMethodScope,
            CodegenClassScope codegenClassScope)
        {
            var block = codegenMethodScope.MakeChild(typeof(object), GetType(), codegenClassScope).AddParam(typeof(object), "value")
                .Block
                .IfRefNotTypeReturnConst("value", typeof(IDictionary<object, object>), "null");
            if (nestedGetter is MapEventPropertyGetter) {
                return block.MethodReturn(
                    ((MapEventPropertyGetter) nestedGetter).UnderlyingGetCodegen(
                        Cast(typeof(IDictionary<object, object>), Ref("value")), codegenMethodScope, codegenClassScope));
            }

            return block.MethodReturn(ConstantNull());
        }

        private bool IsExistsProperty(IDictionary<string, object> map)
        {
            var value = map.Get(propertyMap);
            if (value == null || !(value is IDictionary<string, object>)) {
                return false;
            }

            if (nestedGetter is MapEventPropertyGetter) {
                return ((MapEventPropertyGetter) nestedGetter).IsMapExistsProperty((IDictionary<string, object>) value);
            }

            return false;
        }

        private CodegenMethod IsExistsPropertyCodegen(
            CodegenMethodScope codegenMethodScope,
            CodegenClassScope codegenClassScope)
        {
            var block = codegenMethodScope.MakeChild(typeof(bool), GetType(), codegenClassScope)
                .AddParam(typeof(IDictionary<object, object>), "map").Block
                .DeclareVar(typeof(object), "value", ExprDotMethod(Ref("map"), "get", Constant(propertyMap)))
                .IfRefNullReturnFalse("value")
                .IfRefNotTypeReturnConst("value", typeof(IDictionary<object, object>), false);
            if (nestedGetter is MapEventPropertyGetter) {
                return block.MethodReturn(
                    ((MapEventPropertyGetter) nestedGetter).UnderlyingExistsCodegen(
                        Cast(typeof(IDictionary<object, object>), Ref("value")), codegenMethodScope, codegenClassScope));
            }

            return block.MethodReturn(ConstantFalse());
        }

        public override object HandleNestedValueFragment(object value)
        {
            return null;
        }

        public override CodegenExpression HandleNestedValueCodegen(
            CodegenExpression valueExpression,
            CodegenMethodScope codegenMethodScope,
            CodegenClassScope codegenClassScope)
        {
            return LocalMethod(HandleNestedValueCodegen(codegenMethodScope, codegenClassScope), valueExpression);
        }

        public override CodegenExpression HandleNestedValueFragmentCodegen(
            CodegenExpression name,
            CodegenMethodScope codegenMethodScope,
            CodegenClassScope codegenClassScope)
        {
            return ConstantNull();
        }

        public override CodegenExpression EventBeanExistsCodegen(
            CodegenExpression beanExpression,
            CodegenMethodScope codegenMethodScope,
            CodegenClassScope codegenClassScope)
        {
            return UnderlyingExistsCodegen(
                CastUnderlying(typeof(IDictionary<object, object>), beanExpression), codegenMethodScope, codegenClassScope);
        }

        public override CodegenExpression UnderlyingExistsCodegen(
            CodegenExpression underlyingExpression,
            CodegenMethodScope codegenMethodScope,
            CodegenClassScope codegenClassScope)
        {
            return LocalMethod(IsExistsPropertyCodegen(codegenMethodScope, codegenClassScope), underlyingExpression);
        }
    }
} // end of namespace