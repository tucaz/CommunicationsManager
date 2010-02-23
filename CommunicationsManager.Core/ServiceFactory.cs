using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;

namespace CommunicationsManager.Core
{
    /// <summary>
    /// Creates and manages the life cicle of the WCF proxies
    /// </summary>
    public static class ServiceFactory
    {
        private static Dictionary<string, string> _endpointNames;
        private static Dictionary<string, object> _listOfCreatedFactories;

        /// <summary>
        /// Initializes the dictionary used to hold endpoint configuration names
        /// </summary>
        public static void InitializeEndpoints()
        {
            if (null == _endpointNames)
            {
                _endpointNames = new Dictionary<string, string>();
            }
        }

        /// <summary>
        /// Adds to the config cache an endpoint name 
        /// </summary>
        /// <param name="serviceInterfaceName">The name of the interface that the endpoint will be associated</param>
        /// <param name="endpointName">Endpoint used with the provided interface</param>
        public static void AddEndpoint(string serviceInterfaceName, string endpointName)
        {
            lock (_endpointNames)
            {
                _endpointNames.Add(serviceInterfaceName, endpointName);
            }
        }

        /// <summary>
        /// Factories in cache used to create channels
        /// </summary>
        private static Dictionary<string, object> ListOfCreatedFactories
        {
            get
            {
                if (null == _listOfCreatedFactories)
                {
                    _listOfCreatedFactories = new Dictionary<string, object>();
                }

                return _listOfCreatedFactories;
            }
            set
            {
                _listOfCreatedFactories = value;
            }
        }

        /// <summary>
        /// Closes a proxy
        /// </summary>
        /// <param name="channel">Proxy instance to be closed</param>
        public static void CloseChannel(object channel)
        {
            if (channel is ICommunicationObject)
            {
                try
                {
                    ((ICommunicationObject)channel).Close();
                }
                catch (CommunicationException)
                {
                    ((ICommunicationObject)channel).Abort();
                }
                catch (TimeoutException)
                {
                    ((ICommunicationObject)channel).Abort();
                }
                catch (Exception)
                {
                    ((ICommunicationObject)channel).Abort();
                    throw;
                }
            }
        }

        /// <summary>
        /// Creates a service proxy for the given type
        /// </summary>
        /// <typeparam name="T">Service Type</typeparam>
        /// <returns>An opened proxy ready for use</returns>
        public static T CreateServiceChannel<T>()
        {
            string key = typeof(T).Name;
            string endpointName = GetEndpointName(key);

            return CreateServiceChannel<T>(endpointName);
        }

        /// <summary>
        /// Creates a service proxy for the given type
        /// </summary>
        /// <typeparam name="T">Service Type</typeparam>
        /// <returns>An opened proxy ready for use</returns>
        public static T CreateServiceChannel<T>(string endpointName)
        {
            string key = typeof(T).Name;

            if (!ListOfCreatedFactories.ContainsKey(key))
            {
                lock (ListOfCreatedFactories)
                {
                    ListOfCreatedFactories.Add(key, new ChannelFactory<T>(GetEndpointName(key)));
                }
            }

            T channel = ((ChannelFactory<T>)ListOfCreatedFactories[key]).CreateChannel();
            ((IClientChannel)channel).Open();

            return channel;
        } 

        /// <summary>
        /// Gets an endpoint name from the cache
        /// </summary>
        /// <param name="key">Service key used to associate the endpoint name</param>
        /// <returns>Returns the endpoint name to be used</returns>
        private static string GetEndpointName(string key)
        {
            if (null == _endpointNames)
            {
                throw new InvalidOperationException("Endpoint configuration was not initialized. Call InitializeEndpoints() method before using the factory.");
            }
            else
            {
                if (!_endpointNames.ContainsKey(key))
                {
                    throw new InvalidOperationException(String.Format("The endpoint name for service interface {0} was not found. Call AddEndpoint() method first before using the factory", key));
                }
                else
                {
                    return _endpointNames[key];
                }
            }
        }
    }
}
