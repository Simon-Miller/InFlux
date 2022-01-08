using System.Reflection;

namespace InFlux
{
    /// <summary>
    /// represents an entity that shouts about member values changing.
    /// Automatically signs up to value change events on the inheritor's members
    /// of type <see cref="QueuedEventProperty{T}"/>.
    /// </summary>
    public abstract class QueuedEventsEntity<T> where T: QueuedEventsEntity<T>
    {      
        public QueuedEventsEntity()
        {
            // scan self for members implementing IQueuedEventProperty;

            var matchingFieldMembers = this.GetType()
                    .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Where(x => x.FieldType.IsGenericType)
                    .Where(f => f.FieldType.IsAssignableTo(typeof(QueuedEventPropertyBase)));


            var matchingPropertyMembers = this.GetType()
                    .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Where(x => x.PropertyType.IsGenericType)
                    .Where(p => p.PropertyType.IsAssignableTo(typeof(QueuedEventPropertyBase)));

            foreach (var match in matchingFieldMembers)
                this.setupMember(match.GetValue(this));                
            
            foreach(var match in matchingPropertyMembers)
                this.setupMember(match.GetValue(this));
        }

        /// <summary>
        /// should fire when any <see cref="QueuedEventProperty{T}"/> member of this instance changes.
        /// The idea being you create your own strongly typed event where you can pass on your entity
        /// to your subscribers so they know which entity changed.
        /// </summary>
        public readonly QueuedEvent<T> EntityChanged = new();

        private void onMemberValueChanged() => this.EntityChanged.FireEvent((T)this);
        
        private void setupMember(object? member)
        {
            if (member != null)
                ((QueuedEventPropertyBase)member).OnValueChanged(() => this.onMemberValueChanged());           
        }    
    }
}
