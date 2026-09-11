using VitaCernita.Core.Queries;

namespace VitaCernita.Core.Filters;

/// <summary>
/// Legacy interface representing a condition in a Gmail filter query.
/// Inherits from <see cref="IQueryCondition"/> for full backward compatibility.
/// </summary>
public interface IFilterCondition : IQueryCondition
{
}
