using System;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;

namespace DeployToAzure.Utility
{
    public static class ExpressionHelpers
    {
        public static FieldInfo GetFieldInfo<TOwner, TField>(this Expression<Func<TOwner, TField>> fieldRef)
        {
            var memberExpr = fieldRef.Body as MemberExpression;
            if (memberExpr == null)
                throw new InvalidExpressionException("Expected a MemberExpression at the top level.");

            var fieldInfo = memberExpr.Member as FieldInfo;
            if (fieldInfo == null)
                throw new InvalidExpressionException("Expected the MemberExpression to reference a field.");

            return fieldInfo;
        }

        public static string GetPropertyName<TOwner, TProperty>(this Expression<Func<TOwner, TProperty>> propertyRef)
        {
            var body = propertyRef.Body;

            var unaryExpression = body as UnaryExpression;
            if (unaryExpression != null && unaryExpression.NodeType == ExpressionType.Convert)
                body = unaryExpression.Operand;

            var memberExpr = body as MemberExpression;
            if (memberExpr == null)
                throw new InvalidExpressionException("Expected a MemberExpression at the top level.");

            var propertyInfo = memberExpr.Member as PropertyInfo;
            if (propertyInfo == null)
                throw new InvalidExpressionException("Expected the MemberExpression to reference a property.");

            return propertyInfo.Name;
        }

        public static string GetPropertyName<TProperty>(this Expression<Func<TProperty>> propertyRefClosure)
        {
            var memberExpr = propertyRefClosure.Body as MemberExpression;
            if (memberExpr == null)
                throw new InvalidExpressionException("Expected a MemberExpression at the top level.");

            var propertyInfo = memberExpr.Member as PropertyInfo;
            if (propertyInfo == null)
                throw new InvalidExpressionException("Expected the MemberExpression to reference a property.");

            return propertyInfo.Name;
        }

        public static string GetPropertyName(this Expression<Func<object>> propertyRefClosure)
        {
            var root = propertyRefClosure.Body;
            var rootConvert = root as UnaryExpression;
            if (rootConvert != null && rootConvert.NodeType == ExpressionType.Convert)
                root = rootConvert.Operand;

            var memberExpr = root as MemberExpression;
            if (memberExpr == null)
                throw new InvalidExpressionException("Expected a MemberExpression at the top level.");

            var propertyInfo = memberExpr.Member as PropertyInfo;
            if (propertyInfo == null)
                throw new InvalidExpressionException("Expected the MemberExpression to reference a property.");

            return propertyInfo.Name;
        }

        public static string GetMethodName<TReturn>(this Expression<Func<TReturn>> methodRefClosure)
        {
            var root = methodRefClosure.Body;
            var methodCallExpression = root as MethodCallExpression;
            if (methodCallExpression == null)
                throw new InvalidExpressionException("Expected a MethodCallExpression at the top level.");

            var methodInfo = methodCallExpression.Method;
            if (methodInfo == null)
                throw new InvalidExpressionException("Expected the MethodCallExpression to reference a method.");

            return methodInfo.Name;
        }

        public static string GetMethodName<TObject, TReturn>(this Expression<Func<TObject, TReturn>> methodRefClosure)
        {
            return methodRefClosure.GetMethodInfo().Name;
        }

        public static MethodInfo GetMethodInfo<TObject, TReturn>(this Expression<Func<TObject, TReturn>> methodRefClosure)
        {
            var root = methodRefClosure.Body;
            var methodCallExpression = root as MethodCallExpression;
            if (methodCallExpression == null)
                throw new InvalidExpressionException("Expected a MethodCallExpression at the top level.");

            var methodInfo = methodCallExpression.Method;
            if (methodInfo == null)
                throw new InvalidExpressionException("Expected the MethodCallExpression to reference a method.");

            return methodInfo;
        }

    }
}