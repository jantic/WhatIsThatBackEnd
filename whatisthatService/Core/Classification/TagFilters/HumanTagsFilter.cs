using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace whatisthatService.Core.Classification.TagFilters
{
    class HumanTagsFilter : ITagsFilter
    {
        //Hope to find a better way of handling these eventually!
        private static readonly ImmutableHashSet<String> HumanTags = ImmutableHashSet.Create("person","boy","girl","adult","teenager","toddler",
            "baby","family","infant","child","man","woman","soldier","athlete","children","farmers","farmer","group",
            "elderly", "professional","brunette", "party", "businessman", "businessmen", "dancer", "musician", "lady", "worker", "collague",
            "businesswoman", "businesswomen","commando","cowboy","infancy","orphan", "guard", "guardian", "government official",
            "spectators", "driver", "buddhist", "barber", "crowd","men","women","eskimo","friends","golfer","hipster","homeless","human",
            "hunter","infantry","journalist","kids", "medical practitioner", "military personnel", "motorcyclist", "player", "police",
            "politician", "president", "senior", "surgeon", "swimmer", "team", "warrior", "female", "male", "leader", "love", "wedding",
            "musician", "couple", "friendship", "silhouette","runner","swimmer", "soccer", "people", "sport", "team sport", "mourner",
            "monk", "leadership", "coach", "buddha", "ancestor", "santa claus", "clown", "actor", "mayor", "head of state", "employee", "actress",
            "criminal");

        public List<ImageTag> Filter(List<ImageTag> tags)
        {
            var tagsArray = new ImageTag[tags.Count];
            tags.CopyTo(tagsArray);
            var augmentedTagsList = new List<ImageTag>(tagsArray);
            augmentedTagsList.AddRange(from tag in tags where HumanTags.Contains(tag.Name.Trim().ToLower()) select new ImageTag("human", tag.Probability));
            return augmentedTagsList;
        }
    }
}
