import React, { useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { campaignService } from '../services/api';
import { Campaign, CampaignSendLog, CampaignStatus } from '../types';
import { ChevronLeft, Edit, RefreshCw } from 'lucide-react';

const CampaignDetail: React.FC = () => {
  const { id } = useParams();
  const navigate = useNavigate();
  const [campaign, setCampaign] = useState<Campaign | null>(null);
  const [logs, setLogs] = useState<CampaignSendLog[]>([]);
  const [loading, setLoading] = useState(true);

  const loadData = async () => {
    if (!id) return;
    try {
      const [campData, logsData] = await Promise.all([
        campaignService.getById(id),
        campaignService.getLogs(id),
      ]);
      setCampaign(campData);
      setLogs(logsData);
    } catch (error) {
      console.error('Failed to load detail', error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData();
    // Simple polling if sending to update progress
    const interval = setInterval(() => {
        if (campaign?.status === CampaignStatus.Sending) {
            loadData();
        }
    }, 5000);
    return () => clearInterval(interval);
  }, [id, campaign?.status]);

  if (loading || !campaign) return <div className="p-10 text-center">Loading details...</div>;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
         <div className="flex items-center gap-3">
             <button
                onClick={() => navigate(-1)}
                className="p-2 hover:bg-gray-200 rounded-full transition-colors text-gray-500"
             >
                <ChevronLeft size={20} />
             </button>
             <div>
                <h1 className="text-2xl font-bold text-gray-900">{campaign.name}</h1>
                <p className="text-sm text-gray-500 flex items-center gap-2">
                    Status: <span className="font-medium text-gray-900">{campaign.status}</span>
                    {campaign.status === CampaignStatus.Sending && <RefreshCw size={12} className="animate-spin" />}
                </p>
             </div>
         </div>
         {campaign.status === CampaignStatus.Draft && (
             <Link
                to={`/campaigns/${campaign.id}/edit`}
                className="flex items-center gap-2 bg-white border border-gray-300 text-gray-700 px-4 py-2 rounded-lg hover:bg-gray-50 transition-colors shadow-sm"
             >
                <Edit size={16} />
                Edit Campaign
             </Link>
         )}
      </div>

      {/* Overview Cards */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <div className="bg-white p-4 rounded-xl border border-gray-200 shadow-sm">
            <div className="text-sm text-gray-500 mb-1">Total Recipients</div>
            <div className="text-2xl font-bold">{campaign.totalRecipients}</div>
        </div>
        <div className="bg-white p-4 rounded-xl border border-gray-200 shadow-sm">
            <div className="text-sm text-gray-500 mb-1">Sent</div>
            <div className="text-2xl font-bold text-green-600">{campaign.sentCount}</div>
        </div>
        <div className="bg-white p-4 rounded-xl border border-gray-200 shadow-sm">
            <div className="text-sm text-gray-500 mb-1">Failed</div>
            <div className="text-2xl font-bold text-red-600">{campaign.failedCount}</div>
        </div>
        <div className="bg-white p-4 rounded-xl border border-gray-200 shadow-sm">
            <div className="text-sm text-gray-500 mb-1">Completion</div>
            <div className="text-2xl font-bold text-brand-blue">
                {campaign.totalRecipients > 0
                    ? Math.round(((campaign.sentCount + campaign.failedCount) / campaign.totalRecipients) * 100)
                    : 0
                }%
            </div>
        </div>
      </div>

      {/* Info & Logs Grid */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        
        {/* Detail Panel */}
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6 space-y-4 h-fit">
            <h3 className="font-semibold text-gray-800 border-b border-gray-100 pb-2">Configuration</h3>
            <div>
                <label className="text-xs font-bold text-gray-400 uppercase">Subject</label>
                <p className="text-gray-900">{campaign.subject}</p>
            </div>
            <div>
                <label className="text-xs font-bold text-gray-400 uppercase">From</label>
                <p className="text-gray-900">{campaign.fromName} &lt;{campaign.fromEmail}&gt;</p>
            </div>
             <div>
                <label className="text-xs font-bold text-gray-400 uppercase">Created At</label>
                <p className="text-gray-900">{new Date(campaign.createdAt).toLocaleString()}</p>
            </div>
        </div>

        {/* Logs Panel */}
        <div className="lg:col-span-2 bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden flex flex-col max-h-[600px]">
            <div className="p-4 border-b border-gray-100 font-semibold text-gray-800 flex justify-between items-center">
                <span>Send Logs</span>
                <button onClick={loadData} className="text-gray-400 hover:text-brand-blue transition-colors">
                    <RefreshCw size={16} />
                </button>
            </div>
            <div className="overflow-y-auto flex-1 p-0">
                <table className="w-full text-left text-sm text-gray-600">
                    <thead className="bg-gray-50 text-gray-500 uppercase font-medium text-xs sticky top-0">
                        <tr>
                            <th className="px-6 py-3">Recipient</th>
                            <th className="px-6 py-3">Status</th>
                            <th className="px-6 py-3">Message</th>
                            <th className="px-6 py-3 text-right">Time</th>
                        </tr>
                    </thead>
                    <tbody className="divide-y divide-gray-100">
                        {logs.length === 0 ? (
                            <tr>
                                <td colSpan={4} className="px-6 py-8 text-center text-gray-400 italic">
                                    No logs available yet.
                                </td>
                            </tr>
                        ) : (
                            logs.map((log) => (
                                <tr key={log.id} className="hover:bg-gray-50">
                                    <td className="px-6 py-3 font-medium text-gray-900">{log.recipientEmail}</td>
                                    <td className="px-6 py-3">
                                        <span className={`px-2 py-0.5 rounded text-xs font-medium ${
                                            log.status === 'Sent' ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'
                                        }`}>
                                            {log.status}
                                        </span>
                                    </td>
                                    <td className="px-6 py-3 text-red-500 text-xs max-w-xs truncate">
                                        {log.errorMessage || '-'}
                                    </td>
                                    <td className="px-6 py-3 text-right text-gray-400 text-xs">
                                        {log.sentAt ? new Date(log.sentAt).toLocaleTimeString() : '-'}
                                    </td>
                                </tr>
                            ))
                        )}
                    </tbody>
                </table>
            </div>
        </div>

      </div>
    </div>
  );
};

export default CampaignDetail;
