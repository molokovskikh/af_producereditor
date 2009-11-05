using System;
using System.Collections;
using System.ComponentModel;
using System.Reflection;
using NHibernate;
using NHibernate.Properties;
using NHibernate.Transform;

namespace ProducerEditor.Service
{
	[Serializable]
	public class AliasToBeanResultTransformer : IResultTransformer
	{
		private const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
		private readonly Type resultClass;
		private ISetter[] setters;
		private readonly IPropertyAccessor propertyAccessor;
		private readonly ConstructorInfo constructor;

		public AliasToBeanResultTransformer(Type resultClass)
		{
			if (resultClass == null)
			{
				throw new ArgumentNullException("resultClass");
			}
			this.resultClass = resultClass;

			constructor = resultClass.GetConstructor(flags, null, Type.EmptyTypes, null);

			// if resultClass is a ValueType (struct), GetConstructor will return null... 
			// in that case, we'll use Activator.CreateInstance instead of the ConstructorInfo to create instances
			if (constructor == null && resultClass.IsClass)
			{
				throw new ArgumentException("The target class of a AliasToBeanResultTransformer need a parameter-less constructor",
					"resultClass");
			}

			propertyAccessor =
				new ChainedPropertyAccessor(new[] {
					PropertyAccessorFactory.GetPropertyAccessor(null),
					PropertyAccessorFactory.GetPropertyAccessor("field")
				});
		}

		public object TransformTuple(object[] tuple, String[] aliases)
		{
			object result;

			try
			{
				if (setters == null)
				{
					setters = new ISetter[aliases.Length];
					for (int i = 0; i < aliases.Length; i++)
					{
						string alias = aliases[i];
						if (alias != null)
						{
							setters[i] = propertyAccessor.GetSetter(resultClass, alias);
						}
					}
				}
				
				// if resultClass is not a class but a value type, we need to use Activator.CreateInstance
				result = resultClass.IsClass
					? constructor.Invoke(null)
					: NHibernate.Cfg.Environment.BytecodeProvider.ObjectsFactory.CreateInstance(resultClass, true);

				for (int i = 0; i < aliases.Length; i++)
				{
					var setter = setters[i];
					var value = tuple[i];
					if (setter != null)
					{
						if (value == null)
						{
							setter.Set(result, value);
							continue;
						}

						var propertyType = setter.Method.GetParameters()[0].ParameterType;
						var valueType = value.GetType();
						if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
							propertyType = propertyType.GetGenericArguments()[0];

						if (valueType == propertyType)
							setter.Set(result, value);
						else
						{
							var converter = TypeDescriptor.GetConverter(valueType);
							setter.Set(result, converter.ConvertTo(value, propertyType));
						}
					}
				}
			}
			catch (InstantiationException e)
			{
				throw new HibernateException("Could not instantiate result class: " + resultClass.FullName, e);
			}
			catch (MethodAccessException e)
			{
				throw new HibernateException("Could not instantiate result class: " + resultClass.FullName, e);
			}

			return result;
		}

		public IList TransformList(IList collection)
		{
			return collection;
		}
	}
}