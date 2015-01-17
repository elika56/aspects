﻿using System;
using System.Linq;
using Common.Logging;
using Newtonsoft.Json;
using PostSharp.Aspects;
using System.Diagnostics;
using System.Threading;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace PubComp.Aspects.Monitoring
{
    [Serializable]
    public class LogAttribute : MethodInterceptionAspect
    {
        private string fullMethodName;
        private readonly LogLevel exceptionLogLevel;
        private ILog log;
        private readonly bool doLogValuesOnException;

        /// <summary>
        /// Creates a new LogAttribute
        /// </summary>
        /// <param name="logName">Name of logger (from Common.Logging) to use, defaults to full class name of decorated class</param>
        /// <param name="exceptionLogLevel">Log level to use in case of exception, defaults to Error</param>
        /// <param name="doLogValuesOnException">Do log values of parameters passed to method in case of exception, defaults to true</param>
        /// <remarks>
        /// Entries and exists are logged with Trace log level.
        /// Exceptions are rethrown (using throw;)
        /// </remarks>
        public LogAttribute(string logName = null, LogLevel exceptionLogLevel = LogLevel.Error, bool doLogValuesOnException = true)
        {
            if (!string.IsNullOrEmpty(logName))
                this.log = LogManager.GetLogger(logName);

            this.exceptionLogLevel = exceptionLogLevel;
            this.doLogValuesOnException = doLogValuesOnException;
        }

        public override void CompileTimeInitialize(System.Reflection.MethodBase method, AspectInfo aspectInfo)
        {
            // ReSharper disable once PossibleNullReferenceException
            var className = method.DeclaringType.FullName;

            if (this.log == null)
                this.log = LogManager.GetLogger(className);

            var parameterTypes = string.Join(", ", method.GetParameters().Select(p => p.ParameterType.FullName).ToArray());

            this.fullMethodName = string.Concat(className, '.', method.Name, '(', parameterTypes, ')');
        }

        public override void OnInvoke(MethodInterceptionArgs args)
        {
            if (this.log == null)
            {
                base.OnInvoke(args);
                return;
            }

            log.Trace(string.Concat("Entering method: ", this.fullMethodName));

            try
            {
                base.OnInvoke(args);
                log.Trace(string.Concat("Exiting method: ", this.fullMethodName));
            }
            catch (Exception ex)
            {
                string message = doLogValuesOnException
                    ? string.Concat("Exception in method: ", this.fullMethodName, ", values: ",
                            JsonConvert.SerializeObject(args.Arguments.ToArray()))
                    : string.Concat("Exception in method: ", this.fullMethodName);

                switch (this.exceptionLogLevel)
                {
                    case LogLevel.Fatal:
                        log.Fatal(message, ex);
                        break;
                    case LogLevel.Error:
                        log.Error(message, ex);
                        break;
                    case LogLevel.Warn:
                        log.Warn(message, ex);
                        break;
                    case LogLevel.Info:
                        log.Info(message, ex);
                        break;
                    case LogLevel.Debug:
                        log.Debug(message, ex);
                        break;
                    default:
                        log.Trace(message, ex);
                        break;
                }

                throw;
            }
        }
    }
}