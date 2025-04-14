// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: Protos/xiangqi.proto
// </auto-generated>
#pragma warning disable 0414, 1591, 8981, 0612
#region Designer generated code

using grpc = global::Grpc.Core;

namespace ChessServer {
  public static partial class ChessService
  {
    static readonly string __ServiceName = "xiangqi.ChessService";

    [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
    static void __Helper_SerializeMessage(global::Google.Protobuf.IMessage message, grpc::SerializationContext context)
    {
      #if !GRPC_DISABLE_PROTOBUF_BUFFER_SERIALIZATION
      if (message is global::Google.Protobuf.IBufferMessage)
      {
        context.SetPayloadLength(message.CalculateSize());
        global::Google.Protobuf.MessageExtensions.WriteTo(message, context.GetBufferWriter());
        context.Complete();
        return;
      }
      #endif
      context.Complete(global::Google.Protobuf.MessageExtensions.ToByteArray(message));
    }

    [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
    static class __Helper_MessageCache<T>
    {
      public static readonly bool IsBufferMessage = global::System.Reflection.IntrospectionExtensions.GetTypeInfo(typeof(global::Google.Protobuf.IBufferMessage)).IsAssignableFrom(typeof(T));
    }

    [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
    static T __Helper_DeserializeMessage<T>(grpc::DeserializationContext context, global::Google.Protobuf.MessageParser<T> parser) where T : global::Google.Protobuf.IMessage<T>
    {
      #if !GRPC_DISABLE_PROTOBUF_BUFFER_SERIALIZATION
      if (__Helper_MessageCache<T>.IsBufferMessage)
      {
        return parser.ParseFrom(context.PayloadAsReadOnlySequence());
      }
      #endif
      return parser.ParseFrom(context.PayloadAsNewBuffer());
    }

    [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
    static readonly grpc::Marshaller<global::ChessServer.ConnectRequest> __Marshaller_xiangqi_ConnectRequest = grpc::Marshallers.Create(__Helper_SerializeMessage, context => __Helper_DeserializeMessage(context, global::ChessServer.ConnectRequest.Parser));
    [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
    static readonly grpc::Marshaller<global::ChessServer.ConnectResponse> __Marshaller_xiangqi_ConnectResponse = grpc::Marshallers.Create(__Helper_SerializeMessage, context => __Helper_DeserializeMessage(context, global::ChessServer.ConnectResponse.Parser));
    [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
    static readonly grpc::Marshaller<global::ChessServer.StartGameRequest> __Marshaller_xiangqi_StartGameRequest = grpc::Marshallers.Create(__Helper_SerializeMessage, context => __Helper_DeserializeMessage(context, global::ChessServer.StartGameRequest.Parser));
    [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
    static readonly grpc::Marshaller<global::ChessServer.StartGameResponse> __Marshaller_xiangqi_StartGameResponse = grpc::Marshallers.Create(__Helper_SerializeMessage, context => __Helper_DeserializeMessage(context, global::ChessServer.StartGameResponse.Parser));
    [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
    static readonly grpc::Marshaller<global::ChessServer.MoveRequest> __Marshaller_xiangqi_MoveRequest = grpc::Marshallers.Create(__Helper_SerializeMessage, context => __Helper_DeserializeMessage(context, global::ChessServer.MoveRequest.Parser));
    [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
    static readonly grpc::Marshaller<global::ChessServer.MoveResponse> __Marshaller_xiangqi_MoveResponse = grpc::Marshallers.Create(__Helper_SerializeMessage, context => __Helper_DeserializeMessage(context, global::ChessServer.MoveResponse.Parser));
    [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
    static readonly grpc::Marshaller<global::ChessServer.ResignRequest> __Marshaller_xiangqi_ResignRequest = grpc::Marshallers.Create(__Helper_SerializeMessage, context => __Helper_DeserializeMessage(context, global::ChessServer.ResignRequest.Parser));
    [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
    static readonly grpc::Marshaller<global::ChessServer.ResignResponse> __Marshaller_xiangqi_ResignResponse = grpc::Marshallers.Create(__Helper_SerializeMessage, context => __Helper_DeserializeMessage(context, global::ChessServer.ResignResponse.Parser));
    [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
    static readonly grpc::Marshaller<global::ChessServer.GameStateRequest> __Marshaller_xiangqi_GameStateRequest = grpc::Marshallers.Create(__Helper_SerializeMessage, context => __Helper_DeserializeMessage(context, global::ChessServer.GameStateRequest.Parser));
    [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
    static readonly grpc::Marshaller<global::ChessServer.GameStateResponse> __Marshaller_xiangqi_GameStateResponse = grpc::Marshallers.Create(__Helper_SerializeMessage, context => __Helper_DeserializeMessage(context, global::ChessServer.GameStateResponse.Parser));

    [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
    static readonly grpc::Method<global::ChessServer.ConnectRequest, global::ChessServer.ConnectResponse> __Method_Connect = new grpc::Method<global::ChessServer.ConnectRequest, global::ChessServer.ConnectResponse>(
        grpc::MethodType.Unary,
        __ServiceName,
        "Connect",
        __Marshaller_xiangqi_ConnectRequest,
        __Marshaller_xiangqi_ConnectResponse);

    [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
    static readonly grpc::Method<global::ChessServer.StartGameRequest, global::ChessServer.StartGameResponse> __Method_StartGame = new grpc::Method<global::ChessServer.StartGameRequest, global::ChessServer.StartGameResponse>(
        grpc::MethodType.Unary,
        __ServiceName,
        "StartGame",
        __Marshaller_xiangqi_StartGameRequest,
        __Marshaller_xiangqi_StartGameResponse);

    [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
    static readonly grpc::Method<global::ChessServer.MoveRequest, global::ChessServer.MoveResponse> __Method_MakeMove = new grpc::Method<global::ChessServer.MoveRequest, global::ChessServer.MoveResponse>(
        grpc::MethodType.Unary,
        __ServiceName,
        "MakeMove",
        __Marshaller_xiangqi_MoveRequest,
        __Marshaller_xiangqi_MoveResponse);

    [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
    static readonly grpc::Method<global::ChessServer.ResignRequest, global::ChessServer.ResignResponse> __Method_Resign = new grpc::Method<global::ChessServer.ResignRequest, global::ChessServer.ResignResponse>(
        grpc::MethodType.Unary,
        __ServiceName,
        "Resign",
        __Marshaller_xiangqi_ResignRequest,
        __Marshaller_xiangqi_ResignResponse);

    [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
    static readonly grpc::Method<global::ChessServer.GameStateRequest, global::ChessServer.GameStateResponse> __Method_GetGameState = new grpc::Method<global::ChessServer.GameStateRequest, global::ChessServer.GameStateResponse>(
        grpc::MethodType.ServerStreaming,
        __ServiceName,
        "GetGameState",
        __Marshaller_xiangqi_GameStateRequest,
        __Marshaller_xiangqi_GameStateResponse);

    /// <summary>Service descriptor</summary>
    public static global::Google.Protobuf.Reflection.ServiceDescriptor Descriptor
    {
      get { return global::ChessServer.XiangqiReflection.Descriptor.Services[0]; }
    }

    /// <summary>Base class for server-side implementations of ChessService</summary>
    [grpc::BindServiceMethod(typeof(ChessService), "BindService")]
    public abstract partial class ChessServiceBase
    {
      [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
      public virtual global::System.Threading.Tasks.Task<global::ChessServer.ConnectResponse> Connect(global::ChessServer.ConnectRequest request, grpc::ServerCallContext context)
      {
        throw new grpc::RpcException(new grpc::Status(grpc::StatusCode.Unimplemented, ""));
      }

      [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
      public virtual global::System.Threading.Tasks.Task<global::ChessServer.StartGameResponse> StartGame(global::ChessServer.StartGameRequest request, grpc::ServerCallContext context)
      {
        throw new grpc::RpcException(new grpc::Status(grpc::StatusCode.Unimplemented, ""));
      }

      [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
      public virtual global::System.Threading.Tasks.Task<global::ChessServer.MoveResponse> MakeMove(global::ChessServer.MoveRequest request, grpc::ServerCallContext context)
      {
        throw new grpc::RpcException(new grpc::Status(grpc::StatusCode.Unimplemented, ""));
      }

      [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
      public virtual global::System.Threading.Tasks.Task<global::ChessServer.ResignResponse> Resign(global::ChessServer.ResignRequest request, grpc::ServerCallContext context)
      {
        throw new grpc::RpcException(new grpc::Status(grpc::StatusCode.Unimplemented, ""));
      }

      [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
      public virtual global::System.Threading.Tasks.Task GetGameState(global::ChessServer.GameStateRequest request, grpc::IServerStreamWriter<global::ChessServer.GameStateResponse> responseStream, grpc::ServerCallContext context)
      {
        throw new grpc::RpcException(new grpc::Status(grpc::StatusCode.Unimplemented, ""));
      }

    }

    /// <summary>Creates service definition that can be registered with a server</summary>
    /// <param name="serviceImpl">An object implementing the server-side handling logic.</param>
    [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
    public static grpc::ServerServiceDefinition BindService(ChessServiceBase serviceImpl)
    {
      return grpc::ServerServiceDefinition.CreateBuilder()
          .AddMethod(__Method_Connect, serviceImpl.Connect)
          .AddMethod(__Method_StartGame, serviceImpl.StartGame)
          .AddMethod(__Method_MakeMove, serviceImpl.MakeMove)
          .AddMethod(__Method_Resign, serviceImpl.Resign)
          .AddMethod(__Method_GetGameState, serviceImpl.GetGameState).Build();
    }

    /// <summary>Register service method with a service binder with or without implementation. Useful when customizing the service binding logic.
    /// Note: this method is part of an experimental API that can change or be removed without any prior notice.</summary>
    /// <param name="serviceBinder">Service methods will be bound by calling <c>AddMethod</c> on this object.</param>
    /// <param name="serviceImpl">An object implementing the server-side handling logic.</param>
    [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
    public static void BindService(grpc::ServiceBinderBase serviceBinder, ChessServiceBase serviceImpl)
    {
      serviceBinder.AddMethod(__Method_Connect, serviceImpl == null ? null : new grpc::UnaryServerMethod<global::ChessServer.ConnectRequest, global::ChessServer.ConnectResponse>(serviceImpl.Connect));
      serviceBinder.AddMethod(__Method_StartGame, serviceImpl == null ? null : new grpc::UnaryServerMethod<global::ChessServer.StartGameRequest, global::ChessServer.StartGameResponse>(serviceImpl.StartGame));
      serviceBinder.AddMethod(__Method_MakeMove, serviceImpl == null ? null : new grpc::UnaryServerMethod<global::ChessServer.MoveRequest, global::ChessServer.MoveResponse>(serviceImpl.MakeMove));
      serviceBinder.AddMethod(__Method_Resign, serviceImpl == null ? null : new grpc::UnaryServerMethod<global::ChessServer.ResignRequest, global::ChessServer.ResignResponse>(serviceImpl.Resign));
      serviceBinder.AddMethod(__Method_GetGameState, serviceImpl == null ? null : new grpc::ServerStreamingServerMethod<global::ChessServer.GameStateRequest, global::ChessServer.GameStateResponse>(serviceImpl.GetGameState));
    }

  }
}
#endregion
