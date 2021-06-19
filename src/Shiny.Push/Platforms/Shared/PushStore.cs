using System;
using System.Collections.Generic;
using System.Linq;

using Shiny.Stores;


namespace Shiny.Push
{
    public class PushStore : NotifyPropertyChanged
    {
        readonly IKeyValueStore settings;


        public PushStore(IKeyValueStoreFactory factory)
        {
            this.settings = factory.DefaultStore;
        }


        public void AddTag(string tag)
        {
            var tags = this.Tags?.ToList() ?? new List<string>(1);
            tags.Add(tag);
            this.Tags = tags.ToArray();
        }


        public void RemoveTag(string tag)
        {
            if (this.Tags == null)
                return;

            this.Tags = this.Tags
                .Where(x => !x.Equals(tag))
                .ToArray();
        }


        public void Clear()
        {
            this.RegistrationToken = null;
            this.RegistrationTokenDate = null;
            this.Tags = null;
        }


        public string? RegistrationToken
        {
            get => this.settings.Get<string?>(nameof(this.RegistrationToken));
            set => this.settings.SetOrRemove(nameof(this.RegistrationToken), value);
        }


        public DateTime? RegistrationTokenDate
        {
            get => this.settings.Get<DateTime?>(nameof(this.RegistrationTokenDate));
            set => this.settings.SetOrRemove(nameof(this.RegistrationTokenDate), value);
        }


        public string[]? Tags
        {
            get => this.settings.Get<string[]?>(nameof(this.Tags));
            set => this.settings.SetOrRemove(nameof(this.Tags), value);
        }
    }
}
