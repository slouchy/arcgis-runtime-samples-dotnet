using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace ArcGISRuntime.Samples.Shared.Models
{
    public class SearchableTreeNode : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public List<object> Items { get; set; }
        private bool m_IsExpanded;

        public bool IsExpanded
        {
            get { return m_IsExpanded; }
            set
            {
                m_IsExpanded = value;
                PropertyChangedEventHandler pc = PropertyChanged;
                if (pc != null)
                    pc.Invoke(this, new PropertyChangedEventArgs("IsExpanded"));
            }
        }

        public SearchableTreeNode(string name, IEnumerable<object> items)
        {
            Name = name;
            Items = items.ToList();
        }
        public event PropertyChangedEventHandler PropertyChanged;

        public SearchableTreeNode Search(Func<SampleInfo, bool> predicate)
        {
            // Search recursively if node contains sub-trees
            var subTrees = Items.OfType<SearchableTreeNode>()
                .Select(cn => cn.Search(predicate))
                .Where(cn => cn != null)
                .ToArray();
            if (subTrees.Any()) return new SearchableTreeNode(Name, subTrees);

            // If the node contains samples, search those
            var matchingSamples = Items
                .OfType<SampleInfo>()
                .Where(predicate)
                .ToArray();
            if (matchingSamples.Any()) return new SearchableTreeNode(Name, matchingSamples);

            // No matches
            return null;
        }
    }
}
