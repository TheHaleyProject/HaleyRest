namespace Haley.Abstractions {
    /// <summary>
    /// A simple straightforward HTTPclient Wrapper.
    /// </summary>
    public interface IRequest : IRestBase, IRestBase<IRequest> {
        //IClient Client { get; } //Should be set only once
        IRequest DoNotAuthenticate();
        IRequest SetClient(IClient client);
        IRequest InheritHeaders(bool inherit = true);
        IRequest InheritAuthentication(bool inherit_authenticator = true, bool inherit_parameter = true); //Give preference to Authenticator to generate a new token. If authenticator is not available, take the parent token.
    }
}
