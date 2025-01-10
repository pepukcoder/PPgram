using PPgram.MVVM.Models.File;
using PPgram.MVVM.Models.MessageContent;

namespace PPgram.Shared;

class Msg_ResetPreviewer;
class Msg_OpenPreviewer
{
    public required FileContentModel content;
    public required FileModel file;
}
