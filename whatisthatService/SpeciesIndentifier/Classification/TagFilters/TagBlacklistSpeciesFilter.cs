using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace whatisthatService.SpeciesIndentifier.Classification.TagFilters
{
    public class TagBlacklistSpeciesFilter : ITagsFilter
    {
        //Hope to find a better way of handling these eventually!
        private static readonly ImmutableHashSet<String> TagBlacklst = ImmutableHashSet.Create("fur", "sow", "sunshine", "sunny", "sea", "car", "sunrise", "pisa", "sandy", "hobby", "sepia","fly");

        public List<ImageTag> Filter(List<ImageTag> tags)
        {
            Trace.WriteLine(@"Tags: " + String.Join(",",tags.ConvertAll(tag => tag.Name).ToList()));
            return tags.Where(tag => !TagBlacklst.Contains(tag.Name.Trim().ToLower())).ToList();
        }
    }
}
