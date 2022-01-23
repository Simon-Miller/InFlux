using InFlux.Extensions;
using System.Reflection;

namespace InFlux
{
    /// <summary>
    /// represents an entity that shouts about member values changing.
    /// Automatically signs up to value change events on the inheritor's members
    /// of type <see cref="QueuedEventProperty{T}"/>,
    /// and type <see cref="QueuedEventList{T}"/>
    /// </summary>
    public abstract class QueuedEventsEntity<T> where T: QueuedEventsEntity<T>
    {      
        public QueuedEventsEntity()
        {
            // scan self for members inheriting QueuedEventPropertyBase;
            var genericFieldMembers = this.GetType()
                    .GetFields(BindingFlags.Public
                             | BindingFlags.NonPublic 
                             | BindingFlags.Instance)
                    .Where(x => x.FieldType.IsGenericType)
                    .ToList();

            var genericPropertyMembers = this.GetType()
                    .GetProperties(BindingFlags.Public 
                                 | BindingFlags.NonPublic 
                                 | BindingFlags.Instance)
                    .Where(x => x.PropertyType.IsGenericType)
                    .ToList();

            genericFieldMembers.Where(f => f
                .FieldType.IsAssignableTo(typeof(QueuedEventPropertyBase)))
                .Each(member => this.setupMember(member.GetValue(this)));

            genericPropertyMembers.Where(p => p
                .PropertyType.IsAssignableTo(typeof(QueuedEventPropertyBase)))
                .Each(member => this.setupMember(member.GetValue(this)));

            genericFieldMembers
                .Where(f => f.FieldType.GetGenericTypeDefinition() == typeof(QueuedEventList<>))
                .Each(member => this.setupListMember(member.GetValue(this)));

            genericPropertyMembers
                .Where(p => p.PropertyType.GetGenericTypeDefinition() == typeof(QueuedEventList<>))
                .Each(member => this.setupListMember(member.GetValue(this)));
        }

        /// <summary>
        /// should fire when any <see cref="QueuedEventProperty{T}"/> member of this instance changes.
        /// The idea being you create your own strongly typed event where you can pass on your entity
        /// to your subscribers so they know which entity changed.
        /// </summary>
        public readonly QueuedEvent<T> EntityChanged = new();

        //something isn't right here.  or setupMember is wrong too.
        private void onMemberValueChanged(object? oldValue, object? newValue) => this.EntityChanged.FireEvent(default, (T?)this);
        
        private void setupMember(object? member)
        {
            if (member != null)
                ((QueuedEventPropertyBase)member).OnValueChanged((oldValue,newValue) => 
                    this.onMemberValueChanged(oldValue, newValue));           
        }

        private void setupListMember(object? member)
        {
            if(member != null)
            {
                // most of the time I love reflection!  FEEL THE FORCE, LUKE!
                // but sometimes, I hate it!  Its taken about 2 hours to get this call to work.
                // truly, jumping through hoops.

                var memberType = member.GetType();
                var genericArg = memberType.GetGenericArguments()[0];

                var thisGenArgType = this.GetType()?.BaseType?.GenericTypeArguments[0];

                var thisFUllType = typeof(QueuedEventsEntity<>).MakeGenericType(thisGenArgType!);

                var invoker = thisFUllType!.GetMethod(
                                 nameof(setupListMemberGeneric),
                                 BindingFlags.Instance | BindingFlags.NonPublic);

                var genericInvoker = invoker!.MakeGenericMethod(genericArg);

                genericInvoker.Invoke(this, new object[] { member });
            }
        }

        private void setupListMemberGeneric<T2>(QueuedEventList<T2> member)=>
            member?.OnListChanged.Subscribe((O, N) => this.EntityChanged.FireEvent(default, (T?)this));     
    }
}
