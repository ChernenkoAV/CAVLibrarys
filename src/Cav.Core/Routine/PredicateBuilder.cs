using System.Linq.Expressions;

#pragma warning disable CA1062 // Проверить аргументы или открытые методы

namespace Cav;

/// <summary>  
/// Построитель предикатов Expression. Скопипастил с интернетов.
/// </summary>  
public static class PredicateBuilder
{
    /// <summary>
    /// Creates a predicate that evaluates to true.
    /// </summary>
    public static Expression<Func<T, bool>> True<T>() => param => true;

    /// <summary>
    /// Creates a predicate that evaluates to false.
    /// </summary>
    public static Expression<Func<T, bool>> False<T>() => param => false;

    /// <summary>
    /// Creates a predicate expression from the specified lambda expression.
    /// </summary>
    public static Expression<Func<T, bool>> Create<T>(Expression<Func<T, bool>> predicate) => predicate;

    /// <summary>
    /// Combines the first predicate with the second using the logical "and".
    /// </summary>
    public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> first, Expression<Func<T, bool>> second) => first.compose(second, Expression.AndAlso);

    /// <summary>
    /// Combines the first predicate with the second using the logical "or".
    /// </summary>
    public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> first, Expression<Func<T, bool>> second) => first.compose(second, Expression.OrElse);

    /// <summary>
    /// Negates the predicate.
    /// </summary>
    public static Expression<Func<T, bool>> Not<T>(this Expression<Func<T, bool>> expression)
    {
        var negated = Expression.Not(expression.Body);
        return Expression.Lambda<Func<T, bool>>(negated, expression.Parameters);
    }

    /// <summary>
    /// Combines the first expression with the second using the specified merge function.
    /// </summary>
    private static Expression<T> compose<T>(this Expression<T> first, Expression<T> second, Func<Expression, Expression, Expression> merge)
    {
        // zip parameters (map from parameters of second to parameters of first)  
        var map = first.Parameters
            .Select((f, i) => new { f, s = second.Parameters[i] })
            .ToDictionary(p => p.s, p => p.f);

        // replace parameters in the second lambda expression with the parameters in the first  
        var secondBody = ParameterRebinder.ReplaceParameters(map, second.Body);

        // create a merged lambda expression with parameters from the first expression  
        return Expression.Lambda<T>(merge(first.Body, secondBody), first.Parameters);
    }
}

internal class ParameterRebinder : ExpressionVisitor
{
    private readonly Dictionary<ParameterExpression, ParameterExpression> map;

    private ParameterRebinder(Dictionary<ParameterExpression, ParameterExpression> map) => this.map = map ?? [];

    public static Expression ReplaceParameters(Dictionary<ParameterExpression, ParameterExpression> map, Expression exp) => new ParameterRebinder(map).Visit(exp);

    /// <summary>
    /// Перезапись параметров из выражения from в выражение to
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <returns></returns>
    public static Expression ReplaceParameters(LambdaExpression from, LambdaExpression to)
    {
        var map = from.Parameters
            .Select((f, i) => new { f, s = to.Parameters[i] })
            .ToDictionary(p => p.s, p => p.f);

        return ReplaceParameters(map, to.Body);
    }

    protected override Expression VisitParameter(ParameterExpression p)
    {

        if (map.TryGetValue(p, out var replacement))
            p = replacement;

        return base.VisitParameter(p);
    }
}
