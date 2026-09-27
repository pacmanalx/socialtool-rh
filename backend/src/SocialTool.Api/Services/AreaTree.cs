using Microsoft.EntityFrameworkCore;
using SocialTool.Domain.Enums;
using SocialTool.Infrastructure.Persistence;

namespace SocialTool.Api.Services;

// A estrutura inteira em memória (são dezenas ou poucas centenas de áreas): caminho completo e subárvores.
public class AreaTree
{
    public record Node(Guid Id, string Name, AreaKind Kind, Guid? ParentId);

    private readonly Dictionary<Guid, Node> _nodes;
    private readonly ILookup<Guid?, Node> _children;

    private AreaTree(List<Node> nodes)
    {
        _nodes = nodes.ToDictionary(n => n.Id);
        _children = nodes.ToLookup(n => n.ParentId);
    }

    public static async Task<AreaTree> LoadAsync(ApplicationDbContext db) =>
        new(await db.Departments.AsNoTracking()
            .Select(d => new Node(d.Id, d.Name, d.Kind, d.ParentDepartmentId))
            .ToListAsync());

    public IEnumerable<Node> All => _nodes.Values;

    public bool Exists(Guid id) => _nodes.ContainsKey(id);

    public Node? Get(Guid id) => _nodes.GetValueOrDefault(id);

    public IEnumerable<Node> ChildrenOf(Guid? id) => _children[id];

    // "Hinode › Tecnologia › Infraestrutura"
    public string PathOf(Guid id)
    {
        var parts = new List<string>();
        var guard = 0;
        for (var current = Get(id); current != null && guard++ < 50; current = current.ParentId is { } p ? Get(p) : null)
            parts.Insert(0, current.Name);
        return string.Join(" › ", parts);
    }

    // As áreas informadas e tudo o que está abaixo delas.
    public HashSet<Guid> WithDescendants(IEnumerable<Guid> roots)
    {
        var result = new HashSet<Guid>();
        var stack = new Stack<Guid>(roots.Where(Exists));
        while (stack.Count > 0)
        {
            var id = stack.Pop();
            if (!result.Add(id))
                continue;
            foreach (var child in _children[id])
                stack.Push(child.Id);
        }
        return result;
    }

    // Mover "id" para debaixo de "newParent" criaria um ciclo?
    public bool WouldCycle(Guid id, Guid? newParent) =>
        newParent is { } p && WithDescendants([id]).Contains(p);
}
