///////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2006-2019 Esper Team. All rights reserved.                           /
// http://esper.codehaus.org                                                          /
// ---------------------------------------------------------------------------------- /
// The software in this package is published under the terms of the GPL license       /
// a copy of which has been included with this distribution in the license.txt file.  /
///////////////////////////////////////////////////////////////////////////////////////

using com.espertech.esper.common.client;
using com.espertech.esper.common.@internal.bytecodemodel.@base;
using com.espertech.esper.common.@internal.bytecodemodel.model.expression;
using com.espertech.esper.common.@internal.@event.core;
using com.espertech.esper.compat;

using static com.espertech.esper.common.@internal.bytecodemodel.model.expression.CodegenExpressionBuilder;

namespace com.espertech.esper.common.@internal.@event.wrap
{
    public class WrapperUnderlyingPropertyGetter : EventPropertyGetterSPI
    {
        private readonly EventPropertyGetterSPI underlyingGetter;

        public WrapperUnderlyingPropertyGetter(EventPropertyGetterSPI underlyingGetter)
        {
            this.underlyingGetter = underlyingGetter;
        }

        public object Get(EventBean theEvent)
        {
            if (!(theEvent is DecoratingEventBean)) {
                throw new PropertyAccessException("Mismatched property getter to EventBean type");
            }

            var wrapperEvent = (DecoratingEventBean) theEvent;
            var wrappedEvent = wrapperEvent.UnderlyingEvent;
            if (wrappedEvent == null) {
                return null;
            }

            return underlyingGetter.Get(wrappedEvent);
        }

        public bool IsExistsProperty(EventBean eventBean)
        {
            return true; // Property exists as the property is not dynamic (unchecked)
        }

        public object GetFragment(EventBean theEvent)
        {
            if (!(theEvent is DecoratingEventBean)) {
                throw new PropertyAccessException("Mismatched property getter to EventBean type");
            }

            var wrapperEvent = (DecoratingEventBean) theEvent;
            var wrappedEvent = wrapperEvent.UnderlyingEvent;
            if (wrappedEvent == null) {
                return null;
            }

            return underlyingGetter.GetFragment(wrappedEvent);
        }

        public CodegenExpression EventBeanGetCodegen(
            CodegenExpression beanExpression,
            CodegenMethodScope codegenMethodScope,
            CodegenClassScope codegenClassScope)
        {
            return LocalMethod(GetCodegen(codegenMethodScope, codegenClassScope), beanExpression);
        }

        public CodegenExpression EventBeanExistsCodegen(
            CodegenExpression beanExpression,
            CodegenMethodScope codegenMethodScope,
            CodegenClassScope codegenClassScope)
        {
            return ConstantTrue();
        }

        public CodegenExpression EventBeanFragmentCodegen(
            CodegenExpression beanExpression,
            CodegenMethodScope codegenMethodScope,
            CodegenClassScope codegenClassScope)
        {
            return LocalMethod(GetFragmentCodegen(codegenMethodScope, codegenClassScope), beanExpression);
        }

        public CodegenExpression UnderlyingGetCodegen(
            CodegenExpression underlyingExpression,
            CodegenMethodScope codegenMethodScope,
            CodegenClassScope codegenClassScope)
        {
            throw ImplementationNotProvided();
        }

        public CodegenExpression UnderlyingExistsCodegen(
            CodegenExpression underlyingExpression,
            CodegenMethodScope codegenMethodScope,
            CodegenClassScope codegenClassScope)
        {
            return ConstantTrue();
        }

        public CodegenExpression UnderlyingFragmentCodegen(
            CodegenExpression underlyingExpression,
            CodegenMethodScope codegenMethodScope,
            CodegenClassScope codegenClassScope)
        {
            throw ImplementationNotProvided();
        }

        private CodegenMethod GetCodegen(
            CodegenMethodScope codegenMethodScope,
            CodegenClassScope codegenClassScope)
        {
            return codegenMethodScope.MakeChild(typeof(object), GetType(), codegenClassScope)
                .AddParam(typeof(EventBean), "theEvent")
                .Block
                .DeclareVarWCast(typeof(DecoratingEventBean), "wrapperEvent", "theEvent")
                .DeclareVar<EventBean>("wrappedEvent", ExprDotName(Ref("wrapperEvent"), "UnderlyingEvent"))
                .IfRefNullReturnNull("wrappedEvent")
                .MethodReturn(
                    underlyingGetter.EventBeanGetCodegen(Ref("wrappedEvent"), codegenMethodScope, codegenClassScope));
        }

        private CodegenMethod GetFragmentCodegen(
            CodegenMethodScope codegenMethodScope,
            CodegenClassScope codegenClassScope)
        {
            return codegenMethodScope.MakeChild(typeof(object), GetType(), codegenClassScope)
                .AddParam(typeof(EventBean), "theEvent")
                .Block
                .DeclareVarWCast(typeof(DecoratingEventBean), "wrapperEvent", "theEvent")
                .DeclareVar<EventBean>("wrappedEvent", ExprDotName(Ref("wrapperEvent"), "UnderlyingEvent"))
                .IfRefNullReturnNull("wrappedEvent")
                .MethodReturn(
                    underlyingGetter.EventBeanFragmentCodegen(
                        Ref("wrappedEvent"),
                        codegenMethodScope,
                        codegenClassScope));
        }

        private UnsupportedOperationException ImplementationNotProvided()
        {
            return new UnsupportedOperationException(
                "Wrapper event type does not provide an implementation for underlying get");
        }
    }
} // end of namespace