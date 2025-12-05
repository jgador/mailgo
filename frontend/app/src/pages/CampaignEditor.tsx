import React, { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { campaignService } from '../services/api';
import { CampaignStatus, CreateCampaignRequest, SendNowRequest, SendTestRequest } from '../types';
import SmtpModal from '../components/SmtpModal';
import { Save, Send, Eye, Monitor, ChevronLeft } from 'lucide-react';

const CampaignEditor: React.FC = () => {
  const { id: routeId } = useParams();
  const navigate = useNavigate();
  const [campaignId, setCampaignId] = useState<string | null>(routeId ?? null);
  const isEditMode = !!campaignId;

  const [formData, setFormData] = useState<CreateCampaignRequest>({
    name: '',
    subject: '',
    fromName: '',
    fromEmail: '',
    htmlBody: '',
    textBody: '',
  });

  const [campaignStatus, setCampaignStatus] = useState<CampaignStatus>(CampaignStatus.Draft);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [preparingAction, setPreparingAction] = useState<'test' | 'send' | null>(null);
  
  // Modal State
  const [showTestModal, setShowTestModal] = useState(false);
  const [showSendModal, setShowSendModal] = useState(false);

  useEffect(() => {
    if (routeId && routeId !== campaignId) {
      setCampaignId(routeId);
    }
  }, [routeId, campaignId]);

  useEffect(() => {
    if (campaignId) {
      loadCampaign(campaignId);
    }
  }, [campaignId]);

  const loadCampaign = async (campaignId: string) => {
    setLoading(true);
    try {
      const data = await campaignService.getById(campaignId);
      setFormData({
        name: data.name,
        subject: data.subject,
        fromName: data.fromName,
        fromEmail: data.fromEmail,
        htmlBody: data.htmlBody,
        textBody: data.textBody || '',
      });
      setCampaignStatus(data.status);
    } catch (error) {
      console.error('Failed to load campaign', error);
      navigate('/', { replace: true });
    } finally {
      setLoading(false);
    }
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
  };

  const ensureRequiredFields = () => {
    if (!formData.name || !formData.subject || !formData.fromEmail) {
      alert('Please fill in required fields (Name, Subject, From Email)');
      return false;
    }
    return true;
  };

  const ensureCampaignExists = async (): Promise<string | null> => {
    if (campaignId) {
      return campaignId;
    }
    if (!ensureRequiredFields()) {
      return null;
    }

    setSaving(true);
    try {
      const newCampaign = await campaignService.create(formData);
      setCampaignId(newCampaign.id);
      setCampaignStatus(newCampaign.status);
      navigate(`/campaigns/${newCampaign.id}`, { replace: true });
      return newCampaign.id;
    } catch (error) {
      console.error('Failed to create campaign', error);
      alert('Failed to create campaign.');
      return null;
    } finally {
      setSaving(false);
    }
  };

  const handleSave = async () => {
    if (!ensureRequiredFields()) {
      return;
    }

    setSaving(true);
    try {
      if (campaignId) {
        await campaignService.update(campaignId, formData);
      } else {
        await ensureCampaignExists();
      }
    } catch (error) {
      console.error('Failed to save', error);
      alert('Failed to save campaign.');
    } finally {
      setSaving(false);
    }
  };

  const handleSendTest = async (settings: SendTestRequest | SendNowRequest) => {
    const currentId = await ensureCampaignExists();
    if (!currentId || !('testEmail' in settings)) return;
    await campaignService.sendTest(currentId, settings);
    alert('Test email sent successfully!');
  };

  const handleSendNow = async (settings: SendTestRequest | SendNowRequest) => {
     const currentId = await ensureCampaignExists();
     if (!currentId) return;
     const payload: SendNowRequest = {
        smtpHost: settings.smtpHost,
        smtpPort: settings.smtpPort,
        smtpUsername: settings.smtpUsername,
        smtpPassword: settings.smtpPassword,
        encryption: settings.encryption,
     };
     await campaignService.sendNow(currentId, payload);
     alert('Campaign started successfully!');
     navigate(`/campaigns/${currentId}`);
  };

  const handleOpenTestModal = async () => {
    if (isReadOnly) return;
    setPreparingAction('test');
    const ensuredId = await ensureCampaignExists();
    if (ensuredId) {
      setShowTestModal(true);
    }
    setPreparingAction(null);
  };

  const handleOpenSendModal = async () => {
    if (isReadOnly) return;
    setPreparingAction('send');
    const ensuredId = await ensureCampaignExists();
    if (ensuredId) {
      setShowSendModal(true);
    }
    setPreparingAction(null);
  };

  // If not draft, we shouldn't be here really, unless viewing code. 
  // But for now, let's assume we can only view if read-only logic was needed.
  // The PRD says "User can edit... while status is Draft".
  const isReadOnly = campaignStatus !== CampaignStatus.Draft;

  if (loading) return <div className="p-10 text-center">Loading editor...</div>;

  return (
    <div className="h-[calc(100vh-8rem)] flex flex-col">
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <div className="flex items-center gap-3">
            <button onClick={() => navigate(-1)} className="p-2 hover:bg-gray-200 rounded-full transition-colors text-gray-500">
                <ChevronLeft size={20} />
            </button>
            <div>
                <h1 className="text-2xl font-bold text-gray-900">
                    {isEditMode ? 'Edit Campaign' : 'New Campaign'}
                </h1>
                <p className="text-sm text-gray-500">
                    {isReadOnly ? `Read Only (${campaignStatus})` : 'Compose your email'}
                </p>
            </div>
        </div>
        
        <div className="flex items-center gap-2">
            {!isReadOnly && (
                <button
                onClick={handleSave}
                disabled={saving}
                className="flex items-center gap-2 bg-white border border-gray-300 text-gray-700 px-4 py-2 rounded-lg hover:bg-gray-50 transition-colors shadow-sm disabled:opacity-50"
                >
                <Save size={18} />
                {saving ? 'Saving...' : 'Save Draft'}
                </button>
            )}
            
            {!isReadOnly && (
                <>
                <button
                    onClick={handleOpenTestModal}
                    className="flex items-center gap-2 bg-white border border-gray-300 text-gray-700 px-4 py-2 rounded-lg hover:bg-gray-50 transition-colors shadow-sm disabled:opacity-50"
                    disabled={preparingAction === 'test'}
                >
                    <Eye size={18} />
                    {preparingAction === 'test' ? 'Preparing...' : 'Send Test'}
                </button>
                <button
                    onClick={handleOpenSendModal}
                    className="flex items-center gap-2 bg-brand-blue text-white px-4 py-2 rounded-lg hover:bg-brand-blue-dark transition-colors shadow-sm disabled:opacity-50"
                    disabled={preparingAction === 'send'}
                >
                    <Send size={18} />
                    {preparingAction === 'send' ? 'Preparing...' : 'Send Now'}
                </button>
                </>
            )}
        </div>
      </div>

      {/* Main Split Layout */}
      <div className="flex-1 grid grid-cols-1 lg:grid-cols-2 gap-6 min-h-0">
        
        {/* Left Column: Form */}
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm flex flex-col overflow-hidden">
            <div className="p-4 border-b border-gray-100 bg-gray-50 font-semibold text-gray-700 flex items-center gap-2">
                Settings
            </div>
            <div className="p-6 space-y-4 overflow-y-auto flex-1">
                <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Campaign Name</label>
                    <input
                        type="text"
                        name="name"
                        value={formData.name}
                        onChange={handleChange}
                        disabled={isReadOnly}
                        className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand-blue outline-none disabled:bg-gray-100"
                        placeholder="e.g. Monthly Newsletter"
                    />
                </div>
                
                <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Subject Line</label>
                    <input
                        type="text"
                        name="subject"
                        value={formData.subject}
                        onChange={handleChange}
                        disabled={isReadOnly}
                        className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand-blue outline-none disabled:bg-gray-100"
                        placeholder="Great news inside!"
                    />
                </div>

                <div className="grid grid-cols-2 gap-4">
                    <div>
                        <label className="block text-sm font-medium text-gray-700 mb-1">From Name</label>
                        <input
                            type="text"
                            name="fromName"
                            value={formData.fromName}
                            onChange={handleChange}
                            disabled={isReadOnly}
                            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand-blue outline-none disabled:bg-gray-100"
                            placeholder="John Doe"
                        />
                    </div>
                    <div>
                        <label className="block text-sm font-medium text-gray-700 mb-1">From Email</label>
                        <input
                            type="email"
                            name="fromEmail"
                            value={formData.fromEmail}
                            onChange={handleChange}
                            disabled={isReadOnly}
                            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand-blue outline-none disabled:bg-gray-100"
                            placeholder="john@example.com"
                        />
                    </div>
                </div>

                <div className="pt-2">
                    <label className="block text-sm font-medium text-gray-700 mb-1 flex justify-between">
                        <span>HTML Body</span>
                        <span className="text-xs text-gray-400 font-normal">HTML supported</span>
                    </label>
                    <textarea
                        name="htmlBody"
                        value={formData.htmlBody}
                        onChange={handleChange}
                        disabled={isReadOnly}
                        rows={10}
                        className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand-blue outline-none font-mono text-sm disabled:bg-gray-100"
                        placeholder="<h1>Hello!</h1><p>...</p>"
                    />
                </div>

                <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Text Fallback (Optional)</label>
                    <textarea
                        name="textBody"
                        value={formData.textBody}
                        onChange={handleChange}
                        disabled={isReadOnly}
                        rows={4}
                        className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand-blue outline-none font-mono text-sm disabled:bg-gray-100"
                        placeholder="Plain text version..."
                    />
                </div>
            </div>
        </div>

        {/* Right Column: Preview */}
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm flex flex-col overflow-hidden">
             <div className="p-4 border-b border-gray-100 bg-gray-50 font-semibold text-gray-700 flex items-center gap-2">
                <Monitor size={16} />
                Live Preview
            </div>
            <div className="flex-1 bg-gray-100 p-4 overflow-y-auto">
                <div className="bg-white mx-auto max-w-2xl min-h-[500px] shadow-sm rounded-md overflow-hidden">
                    {/* Simulated Email Header */}
                    <div className="border-b border-gray-100 p-4 bg-gray-50 text-sm">
                        <div className="flex mb-1">
                            <span className="text-gray-500 w-16">Subject:</span>
                            <span className="font-medium text-gray-900">{formData.subject || '(No Subject)'}</span>
                        </div>
                        <div className="flex">
                             <span className="text-gray-500 w-16">From:</span>
                             <span className="text-gray-900">
                                {formData.fromName} &lt;{formData.fromEmail}&gt;
                             </span>
                        </div>
                    </div>
                    {/* HTML Content */}
                    <div className="p-6 prose max-w-none">
                        {formData.htmlBody ? (
                            <iframe
                                title="preview"
                                srcDoc={formData.htmlBody}
                                className="w-full h-[600px] border-none"
                                sandbox="allow-same-origin" 
                            />
                        ) : (
                            <div className="text-gray-300 text-center py-20 italic">
                                Start typing HTML content to see a preview...
                            </div>
                        )}
                    </div>
                </div>
            </div>
        </div>
      </div>

      <SmtpModal
        isOpen={showTestModal}
        onClose={() => setShowTestModal(false)}
        onSubmit={handleSendTest}
        isTest={true}
        title="Send Test Email"
      />

      <SmtpModal
        isOpen={showSendModal}
        onClose={() => setShowSendModal(false)}
        onSubmit={handleSendNow}
        isTest={false}
        title="Send Campaign Now"
      />
    </div>
  );
};

export default CampaignEditor;
